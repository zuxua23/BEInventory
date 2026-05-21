namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
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

    public async Task PrepareAsync(
        StockPreparationRequestDto dto,
        string user
    )
    {
        using var trx =
            await _db.Database.BeginTransactionAsync();

        try
        {
            DailyFileLogger.Info(
                $"Starting stock preparation process. DO='{dto.DoId}', ScannerType='{dto.ScannerType}', Code='{dto.Code}'.",
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

            var location = await _db.Locations
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.LocId &&
                    !x.IsDelete
                );

            if (location == null)
            {
                DailyFileLogger.Warn(
                    $"Location with ID '{dto.LocId}' was not found.",
                    user
                );

                throw new Exception(
                    "Location not found."
                );
            }

            Tag? tag;

            if (dto.ScannerType == "RFID")
            {
                tag = await _db.Tags
                    .FirstOrDefaultAsync(t =>
                        t.EpcTag == dto.Code
                    );

                DailyFileLogger.Info(
                    $"RFID scanner detected code '{dto.Code}'.",
                    user
                );
            }
            else
            {
                tag = await _db.Tags
                    .FirstOrDefaultAsync(t =>
                        t.TagId == dto.Code
                    );

                DailyFileLogger.Info(
                    $"QR scanner detected code '{dto.Code}'.",
                    user
                );
            }

            if (tag == null)
            {
                DailyFileLogger.Warn(
                    $"Tag with code '{dto.Code}' was not found.",
                    user
                );

                throw new Exception(
                    "Tag not found."
                );
            }

            if (tag.Status != TagStatus.IN_STOCK)
            {
                DailyFileLogger.Warn(
                    $"Tag '{tag.TagId}' is not in IN_STOCK status. Current status='{tag.Status}'.",
                    user
                );

                throw new Exception(
                    $"Tag '{tag.TagId}' is not in IN_STOCK status."
                );
            }

            var detail = doData.Details
                .FirstOrDefault(d =>
                    d.ItemId == tag.ItemId
                );

            if (detail == null)
            {
                DailyFileLogger.Warn(
                    $"Item '{tag.ItemId}' does not exist in DO '{dto.DoId}'.",
                    user
                );

                throw new Exception(
                    "Item does not exist in delivery order."
                );
            }

            var reservedCount =
                await _db.TransactionDetails
                    .Where(td =>
                        td.ItemId == tag.ItemId &&
                        td.Transaction.TrsType ==
                            TransactionType.STOCK_PREPARATION &&
                        td.Transaction.ReferenceId ==
                            dto.DoId
                    )
                    .CountAsync();

            if (reservedCount >= detail.QtyRequired)
            {
                DailyFileLogger.Warn(
                    $"Required quantity already fulfilled for ItemId '{tag.ItemId}' in DO '{dto.DoId}'.",
                    user
                );

                throw new Exception(
                    "Required quantity already fulfilled for this item."
                );
            }

            var transaction = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = TransactionType.STOCK_PREPARATION,
                ReferenceId = dto.DoId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(transaction);

            _db.TransactionDetails.Add(
                new Transaction_Detail
                {
                    TrdId = Guid.NewGuid().ToString(),
                    TrsId = transaction.TrsId,
                    TagId = tag.Id,
                    ItemId = tag.ItemId
                }
            );

            tag.Status = TagStatus.RESERVED;
            tag.LocationId = location.Id;
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;

            _db.Histories.Add(
                new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = tag.ItemId,
                    Type = HistoryType.STOCK_PREPARATION,
                    Reference = dto.DoId,
                    Action =
                        "RESERVED_TO_" +
                        location.Name
                            .Replace(" ", "_")
                            .ToUpper(),
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                }
            );

            if (doData.Status == DoStatus.DRAFT)
            {
                doData.Status = DoStatus.PREPARATION;

                DailyFileLogger.Info(
                    $"Delivery order '{dto.DoId}' status updated from DRAFT to PREPARATION.",
                    user
                );
            }

            await _db.SaveChangesAsync();

            await trx.CommitAsync();

            DailyFileLogger.Info(
                $"Stock preparation completed successfully. DO='{dto.DoId}', Tag='{tag.TagId}', TransactionId='{transaction.TrsId}'.",
                user
            );

            DailyFileLogger.Audit(
                action: "STOCK_PREPARATION",
                entity: "TAG",
                entityId: tag.TagId,
                performedBy: user,
                description:
                    $"Reserved tag '{tag.TagId}' for DO '{dto.DoId}' at location '{location.Name}'."
            );
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();

            DailyFileLogger.Error(
                "An error occurred during stock preparation process.",
                ex,
                user
            );

            throw;
        }
    }
}