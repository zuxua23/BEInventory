namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
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


        var doData = await _db.DOs
            .Include(d => d.Details)
            .FirstOrDefaultAsync(d => d.DoId == dto.DoId && !d.IsDelete);

        if (doData == null)
            throw new Exception("DO tidak ditemukan");

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
            throw new Exception("Tag tidak ditemukan");

        if (tag.Status != "IN_STOCK")
            throw new Exception($"Tag {tag.TagId} tidak dalam status IN_STOCK");

        var detail = doData.Details
            .FirstOrDefault(d => d.ItemId == tag.ItemId);

        if (detail == null)
            throw new Exception("Item tidak ada dalam DO");

        var reservedCount = await _db.TransactionDetails
            .Where(td =>
                td.ItemId == tag.ItemId &&
                td.Transaction.TrsType == "STOCK_PREPARATION" &&
                td.Transaction.ReferenceId == dto.DoId)
            .CountAsync();

        if (reservedCount >= detail.QtyRequired)
            throw new Exception("Qty item sudah terpenuhi untuk DO ini");

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
        tag.UpdatedBy = user;
        tag.UpdatedAt = DateTime.UtcNow;

    

        _db.Histories.Add(new HistoryPrint
        {
            Id = Guid.NewGuid().ToString(),
            TagId = tag.Id,
            ItemId = tag.ItemId,
            Type = "STOCK_PREPARATION",
            Reference = dto.DoId,
            Action = "RESERVED",
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await trx.CommitAsync();
    }

    //public async Task PrepareAsync(StockPreparationDto dto, string user)
    //{
    //    using var trx = await _db.Database.BeginTransactionAsync();

    //    if (!dto.TagIds.Any())
    //        throw new Exception("Tidak ada tag yang diprepare");

    //    var doData = await _db.DOs
    //        .Include(d => d.Details)
    //        .FirstOrDefaultAsync(d => d.DoId == dto.DoId && d.IsDelete == false);

    //    if (doData == null)
    //        throw new Exception("DO tidak ditemukan");

    //    var tags = await _db.Tags
    //        .Where(t => dto.TagIds.Contains(t.TagId))
    //        .ToListAsync();

    //    foreach (var tag in tags)
    //    {
    //        if (tag.Status != "IN_STOCK")
    //            throw new Exception($"Tag {tag.TagId} belum ada di stock");

    //        var doDetail = doData.Details
    //            .FirstOrDefault(x => x.ItemId == tag.ItemId);

    //        if (doDetail == null)
    //            throw new Exception($"Item {tag.ItemId} tidak ada di DO");

    //        var reservedCount = await _db.Tags
    //            .CountAsync(t => t.Status == "RESERVED"
    //                             && t.ItemId == tag.ItemId);

    //        if (reservedCount >= doDetail.QtyRequired)
    //            throw new Exception($"Qty item {tag.ItemId} sudah terpenuhi");

    //        tag.Status = "RESERVED";
    //        tag.UpdatedBy = user;
    //        tag.UpdatedAt = DateTime.UtcNow;
    //    }

    //    var trxHeader = new Transaction
    //    {
    //        TrsId = Guid.NewGuid().ToString(),
    //        TrsType = "STOCK_PREPARATION",
    //        ReferenceId = dto.DoId,
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
    //            Type = "STOCK_PREPARATION",
    //            Reference = dto.DoId,
    //            Action = "RESERVED",
    //            CreatedBy = user,
    //            CreatedAt = DateTime.UtcNow
    //        });
    //    }

    //    await _db.SaveChangesAsync();
    //    await trx.CommitAsync();
    //}
}
