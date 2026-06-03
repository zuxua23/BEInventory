namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class StockInService : IStockInService
{
    private readonly AppDBContext _db;

    public StockInService(AppDBContext db)
    {
        _db = db;
    }

    public async Task StockInAsync(
        StockInDto dto,
        string user
    )
    {
        using var trx =
            await _db.Database.BeginTransactionAsync();

        try
        {
            DailyFileLogger.Info(
                $"Starting stock in process using scanner type '{dto.ScannerType}'.",
                user
            );

            if (
                dto.ScannedCodes == null ||
                !dto.ScannedCodes.Any()
            )
            {
                DailyFileLogger.Warn(
                    "Stock in failed because no tags were scanned.",
                    user
                );

                throw new Exception(
                    "No tags were scanned."
                );
            }

            List<Tag> tags;
            var scannedCodes = dto.ScannedCodes;

            if (dto.ScannerType == "RFID")
            {
                tags = await _db.Tags
                    .Where(t =>
                        EF.Constant(scannedCodes).Contains(t.EpcTag)
                    )
                    .ToListAsync();

                DailyFileLogger.Info(
                    $"RFID scanner detected {scannedCodes.Count} scanned tag(s).",
                    user
                );
            }
            else
            {
                tags = await _db.Tags
                    .Where(t =>
                        EF.Constant(scannedCodes).Contains(t.TagId)
                    )
                    .ToListAsync();

                DailyFileLogger.Info(
                    $"QR scanner detected {scannedCodes.Count} scanned tag(s).",
                    user
                );
            }

            if (!tags.Any())
            {
                DailyFileLogger.Warn(
                    "Stock in failed because no matching tags were found.",
                    user
                );

                throw new Exception(
                    "Tags not found."
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

            foreach (var tag in tags)
            {
                if (
                    tag.Status != TagStatus.STANDBY &&
                    tag.Status != TagStatus.PRINTED
                )
                {
                    DailyFileLogger.Warn(
                        $"Stock in denied for TagId '{tag.TagId}'. Current status is '{tag.Status}'.",
                        user
                    );

                    throw new Exception(
                        $"Tag '{tag.TagId}' cannot be processed for stock in."
                    );
                }

                tag.Status = TagStatus.IN_STOCK;
                tag.LocationId = location.Id;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                DailyFileLogger.Info(
                    $"Tag '{tag.TagId}' successfully updated to IN_STOCK.",
                    user
                );
            }

            var trxHeader = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = TransactionType.STOCK_IN,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(trxHeader);

            foreach (var tag in tags)
            {
                _db.TransactionDetails.Add(
                    new Transaction_Detail
                    {
                        TrdId = Guid.NewGuid().ToString(),
                        TrsId = trxHeader.TrsId,
                        TagId = tag.Id,
                        ItemId = tag.ItemId
                    }
                );

                _db.Histories.Add(
                    new HistoryPrint
                    {
                        Id = Guid.NewGuid().ToString(),
                        TagId = tag.Id,
                        ItemId = tag.ItemId,
                        Type = HistoryType.STOCK_IN,
                        Reference = trxHeader.TrsId,
                        Action =
                            "MOVE_TO_" +
                            location.Name
                                .Replace(" ", "_")
                                .ToUpper(),
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    }
                );
            }

            await _db.SaveChangesAsync();

            await trx.CommitAsync();

            DailyFileLogger.Info(
                $"Stock in transaction completed successfully. " +
                $"TransactionId='{trxHeader.TrsId}', " +
                $"TotalTags='{tags.Count}'.",
                user
            );

            DailyFileLogger.Audit(
                action: "STOCK_IN",
                entity: "TAG",
                entityId: trxHeader.TrsId,
                performedBy: user,
                description:
                    $"Processed stock in for {tags.Count} tag(s) to location '{location.Name}'."
            );
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();

            DailyFileLogger.Error(
                "An error occurred during stock in process.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task<TagResponseDto?> GetTagByCodeAsync(string code, string scannerType)
    {
        var tag = await _db.Tags
            .AsNoTracking()
            .Where(t => scannerType == "RFID" ? t.EpcTag == code : t.TagId == code)
            .Select(t => new TagResponseDto
            {
                TagId = t.TagId,
                Epc = t.EpcTag,
                ItemName = t.Item.Name,
                Status = t.Status.ToString(),
                Location = t.Location.Name
            })
            .FirstOrDefaultAsync();

        return tag;
    }
  
}