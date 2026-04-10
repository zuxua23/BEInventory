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
            DailyFileLogger.Info($"StockPreparation dimulai. DO: {dto.DoId}, Scanner: {dto.ScannerType}, Code: {dto.Code}");

            var doData = await _db.DOs
                .Include(d => d.Details)
                .FirstOrDefaultAsync(d => d.DoId == dto.DoId);

            if (doData == null)
            {
                DailyFileLogger.Warn($"StockPreparation gagal: DO {dto.DoId} tidak ditemukan");
                throw new Exception("DO tidak ditemukan");
            }

            var location = await _db.Locations
          .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
            {
                DailyFileLogger.Warn($"CreateAsync: Location dengan ID {dto.LocId} tidak ditemukan");
                throw new Exception("Location tidak ditemukan");
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
                DailyFileLogger.Warn($"StockPreparation gagal: Tag {dto.Code} tidak ditemukan");
                throw new Exception("Tag tidak ditemukan");
            }

            if (tag.Status != "IN_STOCK")
            {
                DailyFileLogger.Warn($"StockPreparation gagal: Tag {tag.TagId} status {tag.Status}, harus IN_STOCK");
                throw new Exception($"Tag {tag.TagId} tidak dalam status IN_STOCK");
            }

            var detail = doData.Details
                .FirstOrDefault(d => d.ItemId == tag.ItemId);

            if (detail == null)
            {
                DailyFileLogger.Warn($"StockPreparation gagal: Item {tag.ItemId} tidak ada di DO {dto.DoId}");
                throw new Exception("Item tidak ada dalam DO");
            }

            var reservedCount = await _db.TransactionDetails
                .Where(td =>
                    td.ItemId == tag.ItemId &&
                    td.Transaction.TrsType == "STOCK_PREPARATION" &&
                    td.Transaction.ReferenceId == dto.DoId)
                .CountAsync();

            if (reservedCount >= detail.QtyRequired)
            {
                DailyFileLogger.Warn($"StockPreparation gagal: Qty item {tag.ItemId} sudah terpenuhi di DO {dto.DoId}");
                throw new Exception("Qty item sudah terpenuhi untuk DO ini");
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

            DailyFileLogger.Info($"StockPreparation berhasil. DO: {dto.DoId}, Tag: {tag.TagId}, Transaction: {transaction.TrsId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error di StockPreparationService.PrepareAsync", ex);
            throw;
        }
    }

}
