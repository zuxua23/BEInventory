namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
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

    public async Task StockInAsync(StockInDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            if (dto.ScannedCodes == null || !dto.ScannedCodes.Any())
            {
                DailyFileLogger.Warn("StockInAsync gagal: Tidak ada tag yang discan");
                throw new Exception("Tidak ada tag yang discan");
            }

            List<Tag> tags;

            if (dto.ScannerType == "RFID")
            {
                tags = await _db.Tags
                    .Where(t => dto.ScannedCodes.Contains(t.EpcTag))
                    .ToListAsync();

                DailyFileLogger.Info($"StockInAsync menggunakan scanner RFID. Total scan: {dto.ScannedCodes.Count}");
            }
            else
            {
                tags = await _db.Tags
                    .Where(t => dto.ScannedCodes.Contains(t.TagId))
                    .ToListAsync();

                DailyFileLogger.Info($"StockInAsync menggunakan scanner QR. Total scan: {dto.ScannedCodes.Count}");
            }

            if (!tags.Any())
            {
                DailyFileLogger.Warn("StockInAsync gagal: Tag tidak ditemukan");
                throw new Exception("Tag tidak ditemukan");
            }

            var location = await _db.Locations
                .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
            {
                DailyFileLogger.Warn("StockInAsync gagal: Location WAREHOUSE tidak ditemukan");
                throw new Exception("Location WAREHOUSE tidak ditemukan");
            }

            foreach (var tag in tags)
            {
                if (tag.Status != "STANDBY" && tag.Status != "PRINTED")
                {
                    DailyFileLogger.Warn($"StockInAsync gagal: Tag {tag.TagId} tidak bisa di Stock In. Status: {tag.Status}");
                    throw new Exception($"Tag {tag.TagId} tidak bisa di Stock In");
                }

                tag.Status = "IN_STOCK";
                tag.LocationId = location.Id;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                DailyFileLogger.Info($"Tag {tag.TagId} berhasil diubah status menjadi IN_STOCK");
            }

            var trxHeader = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = "STOCK_IN",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(trxHeader);

            foreach (var tag in tags)
            {
                _db.TransactionDetails.Add(new Transaction_Detail
                {
                    TrdId = Guid.NewGuid().ToString(),
                    TrsId = trxHeader.TrsId,
                    TagId = tag.Id,
                    ItemId = tag.ItemId
                });

                _db.Histories.Add(new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = tag.ItemId,
                    Type = "STOCK_IN",
                    Reference = trxHeader.TrsId,
                    Action = "MOVE_TO_" + location.Name.Replace(" ", "_").ToUpper(),
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"StockInAsync berhasil. TransactionId: {trxHeader.TrsId}, Total Tag: {tags.Count}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error di StockInAsync.", ex);
            throw;
        }
    }
}