namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

public class StockOutService : IStockOutService
{
    private readonly AppDBContext _db;

    public StockOutService(AppDBContext db)
    {
        _db = db;
    }

    public async Task StockOutAsync(StockOutDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        var doData = await _db.DOs
            .Include(d => d.Details)
            .FirstOrDefaultAsync(d => d.DoId == dto.DoId && !d.IsDelete);

        if (doData == null)
            throw new Exception("DO tidak ditemukan");

        var reservedDetails = await _db.TransactionDetails
            .Include(td => td.Tag)
            .Include(td => td.Transaction)
            .Where(td =>
                td.Transaction.TrsType == "STOCK_PREPARATION" &&
                td.Transaction.ReferenceId == dto.DoId)
            .ToListAsync();

        if (!reservedDetails.Any())
            throw new Exception("Tidak ada tag yang diprepare untuk DO ini");

        foreach (var doDetail in doData.Details)
        {
            var reservedCount = reservedDetails
                .Count(x => x.ItemId == doDetail.ItemId);

            if (reservedCount != doDetail.QtyRequired)
                throw new Exception($"Qty item {doDetail.ItemId} belum terpenuhi");
        }

        var trxHeader = new Transaction
        {
            TrsId = Guid.NewGuid().ToString(),
            TrsType = "STOCK_OUT",
            ReferenceId = dto.DoId,
            ReaderId = dto.ReaderId,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(trxHeader);


        foreach (var detail in reservedDetails)
        {
            var tag = detail.Tag;

            if (tag.Status != "RESERVED")
                throw new Exception($"Tag {tag.TagId} tidak dalam status RESERVED");

            tag.Status = "OUT";
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;

            _db.TransactionDetails.Add(new Transaction_Detail
            {
                TrdId = Guid.NewGuid().ToString(),
                TrsId = trxHeader.TrsId,
                TagId = detail.TagId,
                ItemId = detail.ItemId
            });


            _db.Histories.Add(new HistoryPrint
            {
                Id = Guid.NewGuid().ToString(),
                TagId = detail.TagId,
                ItemId = detail.ItemId,
                Type = "STOCK_OUT",
                Reference = dto.DoId,
                Action = "OUT",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });
        }

        doData.Status = "COMPLETED";

        await _db.SaveChangesAsync();
        await trx.CommitAsync();
    }


    public async Task ScanStockOutAsync(string doId, string readerId, string epc, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        var tag = await _db.Tags
            .FirstOrDefaultAsync(t => t.EpcTag == epc);

        if (tag == null)
            return;

        if (tag.Status != "RESERVED")
            return;

        var reserved = await _db.TransactionDetails
            .Include(x => x.Transaction)
            .FirstOrDefaultAsync(x =>
                x.TagId == tag.Id &&
                x.Transaction.TrsType == "STOCK_PREPARATION" &&
                x.Transaction.ReferenceId == doId);

        if (reserved == null)
            return;

        tag.Status = "OUT";
        tag.UpdatedBy = user;
        tag.UpdatedAt = DateTime.UtcNow;

        var trxHeader = new Transaction
        {
            TrsId = Guid.NewGuid().ToString(),
            TrsType = "STOCK_OUT",
            ReferenceId = doId,
            ReaderId = readerId,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(trxHeader);

        _db.TransactionDetails.Add(new Transaction_Detail
        {
            TrdId = Guid.NewGuid().ToString(),
            TrsId = trxHeader.TrsId,
            TagId = tag.Id,
            ItemId = tag.ItemId
        });

        await _db.SaveChangesAsync();
        await trx.CommitAsync();
    }

}