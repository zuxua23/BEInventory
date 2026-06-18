namespace InventoryControl.Service.Implementations;

using System.Collections.Concurrent;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class StockOutService : IStockOutService
{
    private readonly AppDBContext _db;
    public static string? LastInvalidTag { get; set; }

    public StockOutService(AppDBContext db)
    {
        _db = db;
    }


    private static readonly ConcurrentDictionary<string, DateTime> _logThrottleCache = new();
    private const int LOG_THROTTLE_SECONDS = 10;

    private static bool ShouldWriteLog(string key)
    {
        var now = DateTime.UtcNow;

        if (
            _logThrottleCache.TryGetValue(key, out var lastTime) &&
            (now - lastTime).TotalSeconds < LOG_THROTTLE_SECONDS
        )
        {
            return false;
        }

        _logThrottleCache[key] = now;
        return true;
    }

    public class StockOutScanTemp
    {
        public string DoId { get; set; }
        public string ReaderId { get; set; }
        public string TagDbId { get; set; }
        public string TagId { get; set; }
        public string ItemId { get; set; }
        public string Epc { get; set; }
        public DateTime ScannedAt { get; set; }
    }


    public static class StockOutScanSession
    {
        private static readonly ConcurrentDictionary<string, List<StockOutScanTemp>> _sessions = new();

        public static void Add(string doId, StockOutScanTemp scan)
        {
            var list = _sessions.GetOrAdd(doId, _ => new List<StockOutScanTemp>());

            lock (list)
            {
                if (!list.Any(x => x.TagDbId == scan.TagDbId))
                {
                    list.Add(scan);
                }
            }
        }

        public static List<StockOutScanTemp> Get(string doId)
        {
            if (!_sessions.TryGetValue(doId, out var list))
                return new List<StockOutScanTemp>();

            lock (list)
            {
                return list.ToList();
            }
        }

        public static void Clear(string doId)
        {
            _sessions.TryRemove(doId, out _);
        }

    }

    public async Task ScanStockOutAsync( StockOutResponseDto dto,string user)
    {
        try
        {
            var normalizedEpc = dto.Epc
                .Replace(" ", "")
                .Trim()
                .ToUpper();

            var tag = await _db.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.EpcTag.Replace(" ", "").ToUpper() == normalizedEpc
                );

            if (tag == null)
            {
                if (ShouldWriteLog($"NOT_FOUND:{dto.DoId}:{normalizedEpc}"))
                {
                    DailyFileLogger.Warn(
                        $"Tag not found for EPC '{dto.Epc}'.",
                        user
                    );
                }

                return;
            }

            if (tag.Status != TagStatus.RESERVED)
            {
                DailyFileLogger.Warn(
                    $"Tag '{tag.TagId}' is not in RESERVED status.",
                    user
                );

                return;
            }

            DailyFileLogger.Info(
                $"Tag found. TagId='{tag.TagId}', Status='{tag.Status}'.",
                user
            );

            var isValid = await _db.TransactionDetails
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TagId == tag.Id &&
                    x.Transaction.TrsType ==
                        TransactionType.STOCK_PREPARATION &&
                    x.Transaction.ReferenceId ==
                        dto.DoId
                );

            if (!isValid)
            {
                if (ShouldWriteLog($"INVALID:{dto.DoId}:{tag.TagId}"))
                {
                    DailyFileLogger.Warn(
                        $"Tag '{tag.TagId}' is not reserved for DO '{dto.DoId}'.",
                        user
                    );

                    LastInvalidTag = tag.TagId;
                }

                return;
            }
            StockOutScanSession.Add(
                dto.DoId,
                new StockOutScanTemp
                {
                    DoId = dto.DoId,
                    ReaderId = dto.ReaderId,
                    TagDbId = tag.Id,
                    TagId = tag.TagId,
                    ItemId = tag.ItemId,
                    Epc = normalizedEpc,
                    ScannedAt = DateTime.UtcNow
                }
            );

            DailyFileLogger.Info(
                $"Tag '{tag.TagId}' scanned temporarily for stock out. DO='{dto.DoId}'.",
                user
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred during RFID stock out scanning for DO '{dto.DoId}'.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task ConfirmStockOutAsync(string doId, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var scanned = StockOutScanSession.Get(doId);

            if (!scanned.Any())
                throw new Exception("No scanned tags found.");

            var reservedDetails = await _db.TransactionDetails
                .Include(x => x.Transaction)
                .Include(x => x.Tag)
                .Where(x =>
                    x.Transaction.TrsType == TransactionType.STOCK_PREPARATION &&
                    x.Transaction.ReferenceId == doId
                )
                .ToListAsync();

            if (!reservedDetails.Any())
                throw new Exception("No reserved tags found for this delivery order.");

            var reservedTagIds = reservedDetails
                .Select(x => x.TagId)
                .ToHashSet();

            var scannedTagIds = scanned
                .Select(x => x.TagDbId)
                .ToHashSet();

            var missingTags = reservedTagIds
                .Where(x => !scannedTagIds.Contains(x))
                .ToList();

            if (missingTags.Any())
            {
                throw new Exception(
                    $"Cannot confirm stock out. Missing {missingTags.Count} tag(s)."
                );
            }

            var existingStockOut = await _db.Transactions
                .AnyAsync(x =>
                    x.TrsType == TransactionType.STOCK_OUT &&
                    x.ReferenceId == doId
                );

            if (existingStockOut)
                throw new Exception("This delivery order has already been stocked out.");

            var readerId = scanned.First().ReaderId;

            var trxHeader = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = TransactionType.STOCK_OUT,
                ReferenceId = doId,
                ReaderId = readerId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(trxHeader);

            foreach (var detail in reservedDetails)
            {
                var tag = detail.Tag;

                if (tag.Status != TagStatus.RESERVED)
                {
                    throw new Exception(
                        $"Tag '{tag.TagId}' is not in RESERVED status."
                    );
                }

                tag.Status = TagStatus.OUT;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.TransactionDetails.Add(
                    new Transaction_Detail
                    {
                        TrdId = Guid.NewGuid().ToString(),
                        TrsId = trxHeader.TrsId,
                        TagId = detail.TagId,
                        ItemId = detail.ItemId
                    }
                );

                _db.Histories.Add(
                    new HistoryPrint
                    {
                        Id = Guid.NewGuid().ToString(),
                        TagId = detail.TagId,
                        ItemId = detail.ItemId,
                        Type = HistoryType.STOCK_OUT,
                        Reference = doId,
                        Action = "OUT",
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    }
                );
            }

            var doData = await _db.DOs
                .FirstOrDefaultAsync(x => x.DoId == doId && !x.IsDelete);

            if (doData == null)
                throw new Exception("Delivery order not found.");

            doData.Status = DoStatus.COMPLETED;

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            StockOutScanSession.Clear(doId);

            DailyFileLogger.Audit(
                action: "CONFIRM_STOCK_OUT",
                entity: "DELIVERY_ORDER",
                entityId: doId,
                performedBy: user,
                description: $"Confirmed stock out for {reservedDetails.Count} tag(s)."
            );
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();

            DailyFileLogger.Error(
                $"An error occurred during stock out confirmation for DO '{doId}'.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task<List<ItemListDto>> GetItemsAsync( string doId )
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving stock out item progress for DO '{doId}'."
            );
            var scanned = StockOutScanSession.Get(doId);

            var scannedItemCounts = scanned
                .GroupBy(x => x.ItemId)
                .ToDictionary(x => x.Key, x => x.Count());

            var items = await (
                from d in _db.DODetails
                join i in _db.Items
                    on d.ItemId equals i.Id
                where d.DoId == doId
                select new ItemListDto
                {
                    ItemId = d.ItemId,
                    ItemCode = i.ItmId,
                    ItemName = i.Name,
                    Required = d.QtyRequired ?? 0,

                    Reserved = _db.TransactionDetails
                        .Count(td =>
                            td.ItemId == d.ItemId &&
                            td.Transaction.ReferenceId ==
                                doId &&
                            td.Transaction.TrsType ==
                                TransactionType.STOCK_PREPARATION),
                    Scanned = 0
                })
                .ToListAsync();

            foreach (var item in items)
            {
                item.Scanned = scannedItemCounts.ContainsKey(item.ItemId)
                    ? scannedItemCounts[item.ItemId]
                    : 0;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved {items.Count} item progress record(s) for DO '{doId}'."
            );

            return items;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving item progress for DO '{doId}'.",
                ex
            );

            throw;
        }
    }

    public async Task<ProgressDto> GetProgressAsync( string doId)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving stock out progress for DO '{doId}'."
            );

            var total = await _db.TransactionDetails
                .CountAsync(x =>
                    x.Transaction.ReferenceId == doId &&
                    x.Transaction.TrsType == TransactionType.STOCK_PREPARATION);

            var scanned = StockOutScanSession.Get(doId).Count;

            DailyFileLogger.Info(
                $"Progress retrieved successfully for DO '{doId}'. Total='{total}', Scanned='{scanned}'."
            );

            return new ProgressDto
            {
                Total = total,
                Scanned = scanned
            };
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving progress for DO '{doId}'.",
                ex
            );

            throw;
        }
    }

    public async Task<List<TagDto>> GetTagsAsync( string doId)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving scanned tags for DO '{doId}'."
            );

            var scanned = StockOutScanSession.Get(doId);

            return scanned
                .OrderByDescending(x => x.ScannedAt)
                .Select(x => new TagDto
                {
                    TagId = x.TagId,
                    ItemId = x.ItemId
                })
                .ToList();
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving scanned tags for DO '{doId}'.",
                ex
            );

            throw;
        }
    }

    public async Task StockOutAsync(StockOutDto dto, string user)
    {
        await ConfirmStockOutAsync(dto.DoId, user);
    }

    public static class RfidSession
    {
        private static readonly ConcurrentDictionary<string, string>
            _sessions = new();

        public static void Set(
            string readerId,
            string doId
        )
        {
            _sessions[readerId] = doId;
        }

        public static string? Get(string readerId)
        {
            return _sessions.ContainsKey(readerId)
                ? _sessions[readerId]
                : null;
        }

        public static void Remove(string readerId)
        {
            _sessions.TryRemove(
                readerId,
                out _
            );
        }
    }
}