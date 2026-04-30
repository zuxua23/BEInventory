namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class StockPreparationService : IStockPreparationService
{
    private readonly AppDBContext _db;

    public StockPreparationService(AppDBContext db)
    {
        _db = db;
    }

    public async Task PrepareAsync(StockPreparationRequestDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            DailyFileLogger.Info($"StockPreparation started. DO: {dto.DoId}, Scanner: {dto.ScannerType}, Code: {dto.Code}");
            var doData = await _db.DOs
                .Include(d => d.Details)
                .FirstOrDefaultAsync(d => d.DoId == dto.DoId);

            if (doData == null)
            {
                DailyFileLogger.Warn($"StockPreparation failed: DO {dto.DoId} not found");
                throw new Exception("DO not found");
            }

            var location = await _db.Locations
          .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
            {
                DailyFileLogger.Warn($"CreateAsync: Location with ID {dto.LocId} not found");
                throw new Exception("Location not found");
            }

            Tag tag;

            if (dto.ScannerType == "RFID")
            {
                tag = await _db.Tags
                    .FirstOrDefaultAsync(t => t.EpcTag == dto.Code);
            }
            else
            {
                tag = await _db.Tags
                    .FirstOrDefaultAsync(t => t.TagId == dto.Code);
            }

            if (tag == null)
            {
                DailyFileLogger.Warn($"StockPreparation failed: Tag {dto.Code} not found");
                throw new Exception("Tag not found");
            }

            if (tag.Status != "IN_STOCK")
            {
                DailyFileLogger.Warn($"StockPreparation failed: Tag {tag.TagId} status is {tag.Status}, must be IN_STOCK");
                throw new Exception($"Tag {tag.TagId} is not in IN_STOCK status");
            }

            var detail = doData.Details
                .FirstOrDefault(d => d.ItemId == tag.ItemId);

            if (detail == null)
            {
                DailyFileLogger.Warn($"StockPreparation failed: Item {tag.ItemId} not found in DO {dto.DoId}");
                throw new Exception("Item not found in DO");
            }

            var reservedCount = await _db.TransactionDetails
                .Where(td =>
                    td.ItemId == tag.ItemId &&
                    td.Transaction.TrsType == "STOCK_PREPARATION" &&
                    td.Transaction.ReferenceId == dto.DoId)
                .CountAsync();

            if (reservedCount >= detail.QtyRequired)
            {
                DailyFileLogger.Warn($"StockPreparation failed: Qty for item {tag.ItemId} already fulfilled in DO {dto.DoId}");
                throw new Exception("Item quantity already fulfilled for this DO");
            }

            var transaction = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = "STOCK_PREPARATION",
                ReferenceId = dto.DoId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(transaction);

            _db.TransactionDetails.Add(new Transaction_Detail
            {
                TrdId = Guid.NewGuid().ToString(),
                TrsId = transaction.TrsId,
                TagId = tag.Id,
                ItemId = tag.ItemId
            });

            tag.Status = "RESERVED";
            tag.LocationId = location.Id;
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;

            _db.Histories.Add(new HistoryPrint
            {
                Id = Guid.NewGuid().ToString(),
                TagId = tag.Id,
                ItemId = tag.ItemId,
                Type = "STOCK_PREPARATION",
                Reference = dto.DoId,
                Action = "RESERVED_TO_" + location.Name.Replace(" ", "_").ToUpper(),
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });

            if (doData.Status == "DRAFT")
            {
                doData.Status = "PREPARATION";
            }

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"StockPreparation successful. DO: {dto.DoId}, Tag: {tag.TagId}, Transaction: {transaction.TrsId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error di StockPreparationService.PrepareAsync", ex);
            throw;
        }
    }
    public async Task PrepareBulkAsync(StockPreparationBulkRequestDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (dto.ScannedCodes == null || !dto.ScannedCodes.Any())
                throw new Exception("No tags provided");

            DailyFileLogger.Info($"PrepareBulk started. DO: {dto.DoId}, Total: {dto.ScannedCodes.Count}, Scanner: {dto.ScannerType}");

            var doData = await _db.DOs
                .Include(d => d.Details)
                .FirstOrDefaultAsync(d => d.DoId == dto.DoId);

            if (doData == null)
                throw new Exception("DO not found");

            var location = await _db.Locations
                .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
                throw new Exception("Location not found");

            var tags = dto.ScannerType == "RFID"
                ? await _db.Tags.Where(t => dto.ScannedCodes.Contains(t.EpcTag)).ToListAsync()
                : await _db.Tags.Where(t => dto.ScannedCodes.Contains(t.TagId)).ToListAsync();

            if (!tags.Any())
                throw new Exception("Tags not found in database");

            var foundCodes = dto.ScannerType == "RFID"
                ? tags.Select(t => t.EpcTag).ToHashSet()
                : tags.Select(t => t.TagId).ToHashSet();

            var missing = dto.ScannedCodes.Where(c => !foundCodes.Contains(c)).ToList();
            if (missing.Any())
                throw new Exception($"Tags not found: {string.Join(", ", missing)}");
            foreach (var tag in tags)
            {
                if (tag.Status != "IN_STOCK")
                    throw new Exception($"Tag {tag.TagId} status is {tag.Status}, must be IN_STOCK");
                var detail = doData.Details.FirstOrDefault(d => d.ItemId == tag.ItemId);
                if (detail == null)
                    throw new Exception($"Item {tag.ItemId} is not in this DO");
            }

            var reservedPerItem = await _db.TransactionDetails
                .Where(td => td.Transaction.TrsType == "STOCK_PREPARATION"
                          && td.Transaction.ReferenceId == dto.DoId)
                .GroupBy(td => td.ItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ItemId, x => x.Count);

            var newCountPerItem = tags.GroupBy(t => t.ItemId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kv in newCountPerItem)
            {
                var detail = doData.Details.First(d => d.ItemId == kv.Key);
                var alreadyReserved = reservedPerItem.ContainsKey(kv.Key) ? reservedPerItem[kv.Key] : 0;
                var totalAfter = alreadyReserved + kv.Value;

                if (totalAfter > detail.QtyRequired)
                    throw new Exception($"Item {kv.Key} quantity exceeds requirement (scanned: {kv.Value}, already reserved: {alreadyReserved}, required: {detail.QtyRequired})");
            }

            var transaction = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = "STOCK_PREPARATION",
                ReferenceId = dto.DoId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            _db.Transactions.Add(transaction);

            foreach (var tag in tags)
            {
                _db.TransactionDetails.Add(new Transaction_Detail
                {
                    TrdId = Guid.NewGuid().ToString(),
                    TrsId = transaction.TrsId,
                    TagId = tag.Id,
                    ItemId = tag.ItemId
                });

                tag.Status = "RESERVED";
                tag.LocationId = location.Id;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.Histories.Add(new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = tag.ItemId,
                    Type = "STOCK_PREPARATION",
                    Reference = dto.DoId,
                    Action = "RESERVED_TO_" + location.Name.Replace(" ", "_").ToUpper(),
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (doData.Status == "DRAFT")
                doData.Status = "PREPARATION";

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"PrepareBulk successful. DO: {dto.DoId}, Total reserved: {tags.Count}, Trx: {transaction.TrsId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error in PrepareBulkAsync", ex);
            throw;
        }
    }


        public async Task<List<DOResponseDto>> GetDoDraftAsync()
    {
        try
        {
            var result = await _db.DOs
            .Include(x => x.Details)
            .ThenInclude(d => d.Item)
            .Where(x => !x.IsDelete && x.Status == "DRAFT")
            .Select(x => new DOResponseDto
            {
                DoId = x.DoId,
                DoNumber = x.DoNumber,
                ScannerType = x.ScannerType,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                Details = x.Details.Select(d => new DODetailResponseDto
                {
                    DoDetailId = d.DoDetailId,
                    ItemId = d.ItemId,
                    ItemName = d.Item.Name,
                    QtyRequired = d.QtyRequired
                }).ToList()
            })
            .ToListAsync();

            DailyFileLogger.Info($"Successfully retrieved DO data, total: {result.Count}");
            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Failed to retrieve DO data", ex);
            throw;
        }
    }

}
