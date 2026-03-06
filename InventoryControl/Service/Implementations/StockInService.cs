namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
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

        if (!dto.ScannedCodes.Any())
            throw new Exception("Tidak ada tag yang discan");

        List<Tag> tags;

        if (dto.ScannerType == "RFID")
        {
            tags = await _db.Tags
                .Where(t => dto.ScannedCodes.Contains(t.EpcTag))
                .ToListAsync();
        }
        else
        {
            tags = await _db.Tags
                .Where(t => dto.ScannedCodes.Contains(t.TagId))
                .ToListAsync();
        }

        if (!tags.Any())
            throw new Exception("Tag tidak ditemukan");

        var warehouseLocation = await _db.Locations
        .FirstOrDefaultAsync(x => x.LocId == "WAREHOUSE");

        if (warehouseLocation == null)
            throw new Exception("Location WAREHOUSE tidak ditemukan");

        foreach (var tag in tags)
        {
            if (tag.Status != "STANDBY" && tag.Status != "PRINTED")
                throw new Exception($"Tag {tag.TagId} tidak bisa di Stock In");

            tag.Status = "IN_STOCK";
            tag.LocationId = warehouseLocation.Id;
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;
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
                Action = "MOVE_TO_WAREHOUSE",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        await trx.CommitAsync();
    }


    //public async Task StockInAsync(StockInDto dto, string user)
    //{
    //    using var trx = await _db.Database.BeginTransactionAsync();

    //    if (!dto.SelectedTagIds.Any())
    //        throw new Exception("Tidak ada tag yang dipilih");

    //    var selected = dto.SelectedTagIds.OrderBy(x => x);
    //    var scanned = dto.ScannedTagIds.OrderBy(x => x);

    //    if (!selected.SequenceEqual(scanned))
    //        throw new Exception("Semua tag harus discan sebelum save");

    //    var tags = await _db.Tags
    //        .Where(t => dto.SelectedTagIds.Contains(t.TagId))
    //        .ToListAsync();

    //    foreach (var tag in tags)
    //    {
    //        if (tag.Status != "STANDBY")
    //            throw new Exception($"Tag {tag.TagId} tidak dalam status STANDBY");

    //        tag.Status = "IN_STOCK";
    //        tag.UpdatedBy = user;
    //        tag.UpdatedAt = DateTime.UtcNow;
    //    }

    //    var trxHeader = new Transaction
    //    {
    //        TrsId = Guid.NewGuid().ToString(),
    //        TrsType = "STOCK_IN",
    //        //RdrId = dto.ReaderId,
    //        CreatedBy = user,
    //        CreatedAt = DateTime.UtcNow
    //    };

    //    _db.Transactions.Add(trxHeader);

    //    foreach (var tag in tags)
    //    {
    //        _db.TransactionDetails.Add(new Transaction_Detail
    //        {
    //            TrdId = Guid.NewGuid().ToString(),
    //            TrsId = trxHeader.TrsId,
    //            TagId = tag.TagId,
    //            ItemId = tag.ItemId
    //        });

    //        _db.Histories.Add(new HistoryPrint
    //        {
    //            HisId = Guid.NewGuid().ToString(),
    //            TagId = tag.TagId,
    //            ItemId = tag.ItemId,
    //            Type = "STOCK_IN",
    //            Reference = trxHeader.TrsId,
    //            Action = "IN_STOCK",
    //            CreatedBy = user,
    //            CreatedAt = DateTime.UtcNow
    //        });
    //    }

    //    await _db.SaveChangesAsync();
    //    await trx.CommitAsync();
    //}
}