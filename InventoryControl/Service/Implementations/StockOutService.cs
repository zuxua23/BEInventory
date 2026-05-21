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

    public async Task StockOutAsync(
        StockOutDto dto,
        string user
    )
    {
        using var trx =
            await _db.Database.BeginTransactionAsync();

        try
        {
            DailyFileLogger.Info(
                $"Starting stock out process. DO='{dto.DoId}', Reader='{dto.ReaderId}'.",
                user
            );

            var doData = await _db.DOs
                .Include(d => d.Details)
                .FirstOrDefaultAsync(d =>
                    d.DoId == dto.DoId &&
                    !d.IsDelete
                );

            if (doData == null)
            {
                DailyFileLogger.Warn(
                    $"Delivery order '{dto.DoId}' was not found.",
                    user
                );

                throw new Exception(
                    "Delivery order not found."
                );
            }

            var reservedDetails = await _db.TransactionDetails
                .Include(td => td.Tag)
                .Include(td => td.Transaction)
                .Where(td =>
                    td.Transaction.TrsType == TransactionType.STOCK_PREPARATION &&
                    td.Transaction.ReferenceId == dto.DoId
                )
                .ToListAsync();

            if (!reservedDetails.Any())
            {
                DailyFileLogger.Warn(
                    $"No reserved tags found for DO '{dto.DoId}'.",
                    user
                );

                throw new Exception(
                    "No reserved tags found for this delivery order."
                );
            }

            foreach (var doDetail in doData.Details)
            {
                var reservedCount = reservedDetails
                    .Count(x =>
                        x.ItemId == doDetail.ItemId
                    );

                if (reservedCount != doDetail.QtyRequired)
                {
                    DailyFileLogger.Warn(
                        $"Required quantity for ItemId '{doDetail.ItemId}' is not fulfilled.",
                        user
                    );

                    throw new Exception(
                        $"Required quantity for ItemId '{doDetail.ItemId}' is not fulfilled."
                    );
                }
            }

            var trxHeader = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = TransactionType.STOCK_OUT,
                ReferenceId = dto.DoId,
                ReaderId = dto.ReaderId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(trxHeader);

            foreach (var detail in reservedDetails)
            {
                var tag = detail.Tag;

                if (tag.Status != TagStatus.RESERVED)
                {
                    DailyFileLogger.Warn(
                        $"Tag '{tag.TagId}' is not in RESERVED status.",
                        user
                    );

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
                        Reference = dto.DoId,
                        Action = "OUT",
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    }
                );

                DailyFileLogger.Info(
                    $"Tag '{tag.TagId}' successfully processed for stock out.",
                    user
                );
            }

            doData.Status = DoStatus.COMPLETED;

            await _db.SaveChangesAsync();

            await trx.CommitAsync();

            DailyFileLogger.Info(
                $"Stock out completed successfully. DO='{dto.DoId}', TotalTags='{reservedDetails.Count}'.",
                user
            );

            DailyFileLogger.Audit(
                action: "STOCK_OUT",
                entity: "DELIVERY_ORDER",
                entityId: dto.DoId,
                performedBy: user,
                description:
                    $"Processed stock out for {reservedDetails.Count} tag(s)."
            );
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();

            DailyFileLogger.Error(
                "An error occurred during stock out process.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task ScanStockOutAsync(
        StockOutResponseDto dto,
        string user
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"RFID scan received. EPC='{dto.Epc}', DO='{dto.DoId}'.",
                user
            );

            var normalizedEpc =
                dto.Epc.Replace(" ", "");

            var tag = await _db.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.EpcTag.Replace(" ", "") ==
                    normalizedEpc
                );

            if (tag == null)
            {
                DailyFileLogger.Warn(
                    $"Tag not found for EPC '{dto.Epc}'.",
                    user
                );

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
                DailyFileLogger.Warn(
                    $"Tag '{tag.TagId}' is not reserved for DO '{dto.DoId}'.",
                    user
                );

                LastInvalidTag = tag.TagId;

                return;
            }

            var trx = await _db.Transactions
                .FirstOrDefaultAsync(x =>
                    x.TrsType == TransactionType.STOCK_OUT &&
                    x.ReferenceId == dto.DoId
                );

            if (trx == null)
            {
                trx = new Transaction
                {
                    TrsId = Guid.NewGuid().ToString(),
                    TrsType = TransactionType.STOCK_OUT,
                    ReferenceId = dto.DoId,
                    ReaderId = dto.ReaderId,
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Transactions.Add(trx);

                await _db.SaveChangesAsync();

                DailyFileLogger.Info(
                    $"New stock out transaction created. TransactionId='{trx.TrsId}'.",
                    user
                );
            }

            var exists = await _db.TransactionDetails
                .AnyAsync(x =>
                    x.TagId == tag.Id &&
                    x.TrsId == trx.TrsId
                );

            if (exists)
            {
                DailyFileLogger.Warn(
                    $"Duplicate scan detected for TagId '{tag.TagId}'.",
                    user
                );

                return;
            }

            var tagUpdate = new Tag
            {
                Id = tag.Id,
                Status = TagStatus.OUT,
                UpdatedBy = user,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Tags.Attach(tagUpdate);

            _db.Entry(tagUpdate)
                .Property(x => x.Status)
                .IsModified = true;

            _db.TransactionDetails.Add(
                new Transaction_Detail
                {
                    TrdId = Guid.NewGuid().ToString(),
                    TrsId = trx.TrsId,
                    TagId = tag.Id,
                    ItemId = tag.ItemId
                }
            );

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Tag '{tag.TagId}' successfully scanned for stock out.",
                user
            );

            var totalRequired =
                await _db.TransactionDetails
                    .CountAsync(x =>
                        x.Transaction.ReferenceId ==
                            dto.DoId &&
                        x.Transaction.TrsType ==
                            TransactionType.STOCK_PREPARATION
                    );

            var totalScanned =
                await _db.TransactionDetails
                    .CountAsync(x =>
                        x.Transaction.ReferenceId ==
                            dto.DoId &&
                        x.Transaction.TrsType ==
                            TransactionType.STOCK_OUT
                    );

            if (
                totalRequired > 0 &&
                totalRequired == totalScanned
            )
            {
                var doData = await _db.DOs
                    .FirstOrDefaultAsync(x =>
                        x.DoId == dto.DoId
                    );

                if (doData != null)
                {
                    doData.Status = DoStatus.COMPLETED;

                    await _db.SaveChangesAsync();

                    DailyFileLogger.Info(
                        $"Delivery order '{dto.DoId}' marked as COMPLETED.",
                        user
                    );

                    DailyFileLogger.Audit(
                        action: "COMPLETE_STOCK_OUT",
                        entity: "DELIVERY_ORDER",
                        entityId: dto.DoId,
                        performedBy: user,
                        description:
                            $"Completed stock out process for DO '{dto.DoId}'."
                    );
                }
            }
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

    public async Task<List<ItemListDto>> GetItemsAsync(
        string doId
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving stock out item progress for DO '{doId}'."
            );

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

                    Scanned = _db.TransactionDetails
                        .Count(td =>
                            td.ItemId == d.ItemId &&
                            td.Transaction.ReferenceId ==
                                doId &&
                            td.Transaction.TrsType ==
                                TransactionType.STOCK_OUT)
                })
                .ToListAsync();

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

    public async Task<ProgressDto> GetProgressAsync(
        string doId
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving stock out progress for DO '{doId}'."
            );

            var total = await _db.TransactionDetails
                .CountAsync(x =>
                    x.Transaction.ReferenceId ==
                    doId
                );

            var scanned = await _db.TransactionDetails
                .CountAsync(x =>
                    x.Transaction.ReferenceId ==
                        doId &&
                    x.Transaction.TrsType ==
                        TransactionType.STOCK_OUT
                );

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

    public async Task<List<TagDto>> GetTagsAsync(
        string doId
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving scanned tags for DO '{doId}'."
            );

            var tags = await _db.TransactionDetails
                .Where(x =>
                    x.Transaction.ReferenceId ==
                        doId &&
                    x.Transaction.TrsType ==
                        TransactionType.STOCK_OUT
                )
                .Select(x => new TagDto
                {
                    TagId = x.TagId,
                    ItemId = x.ItemId
                })
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {tags.Count} scanned tag(s) for DO '{doId}'."
            );

            return tags;
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