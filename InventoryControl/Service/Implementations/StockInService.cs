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
            var tags = await _db.Tags
                .Where(t => dto.ScannerType == "RFID"
                    ? dto.ScannedCodes.Contains(t.EpcTag)
                    : dto.ScannedCodes.Contains(t.TagId))
                .ToListAsync();

            if (!tags.Any()) throw new Exception("Tag tidak ditemukan di database");

            var location = await _db.Locations.FirstOrDefaultAsync(x => x.Id == dto.LocId);
            if (location == null) throw new Exception("Lokasi tujuan tidak ditemukan");

            foreach (var tag in tags)
            {
                if (tag.Status == "IN_STOCK")
                    throw new Exception($"Tag {tag.TagId} sudah berstatus IN STOCK!");

                if (tag.Status != "STANDBY" && tag.Status != "PRINTED")
                    throw new Exception($"Tag {tag.TagId} statusnya {tag.Status}, tidak bisa di Stock In");
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
                tag.Status = "IN_STOCK";
                tag.LocationId = location.Id;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

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
            DailyFileLogger.Info($"StockIn berhasil: {trxHeader.TrsId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Gagal StockIn.", ex);
            throw; 

        }
    }
    public async Task<TagResponseDto> GetTagByCodeAsync(string code)
    {
        var tag = await _db.Tags
            .Include(t => t.Item)
            .Include(t => t.Location)
            .FirstOrDefaultAsync(t => t.EpcTag == code || t.TagId == code);
       

        if (tag == null) return null;

        return new TagResponseDto
        {
            TagId           = tag.TagId,
            ItemId          = tag.ItemId,
            ItemName        = tag.Item?.Name,   // sesuaikan dengan property Item kamu
            Status          = tag.Status,
            CurrentLocation = tag.Location?.Name    // sesuaikan dengan property Location kamu
        };
    }
}