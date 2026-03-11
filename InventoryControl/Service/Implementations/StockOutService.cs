namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
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

        try
        {
            DailyFileLogger.Info($"StockOut dimulai. DO={dto.DoId}, Reader={dto.ReaderId}");

            var doData = await _db.DOs
                .Include(d => d.Details)
                .FirstOrDefaultAsync(d => d.DoId == dto.DoId && !d.IsDelete);

            if (doData == null)
            {
                DailyFileLogger.Warn($"StockOut gagal: DO {dto.DoId} tidak ditemukan");
                throw new Exception("DO tidak ditemukan");
            }

            var reservedDetails = await _db.TransactionDetails
                .Include(td => td.Tag)
                .Include(td => td.Transaction)
                .Where(td =>
                    td.Transaction.TrsType == "STOCK_PREPARATION" &&
                    td.Transaction.ReferenceId == dto.DoId)
                .ToListAsync();

            if (!reservedDetails.Any())
            {
                DailyFileLogger.Warn($"StockOut gagal: Tidak ada tag reserved untuk DO {dto.DoId}");
                throw new Exception("Tidak ada tag yang diprepare untuk DO ini");
            }

            foreach (var doDetail in doData.Details)
            {
                var reservedCount = reservedDetails
                    .Count(x => x.ItemId == doDetail.ItemId);

                if (reservedCount != doDetail.QtyRequired)
                {
                    DailyFileLogger.Warn($"StockOut gagal: Qty item {doDetail.ItemId} belum terpenuhi");
                    throw new Exception($"Qty item {doDetail.ItemId} belum terpenuhi");
                }
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
                {
                    DailyFileLogger.Warn($"StockOut gagal: Tag {tag.TagId} tidak dalam status RESERVED");
                    throw new Exception($"Tag {tag.TagId} tidak dalam status RESERVED");
                }

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

                DailyFileLogger.Info($"Tag {tag.TagId} berhasil OUT untuk DO {dto.DoId}");
            }

            doData.Status = "COMPLETED";

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"StockOut berhasil. DO={dto.DoId}, TotalTag={reservedDetails.Count}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error di StockOutAsync", ex);
            throw;
        }
    }


    public async Task ScanStockOutAsync(StockOutResponseDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.EpcTag == dto.Epc);

            if (tag == null)
            {
                DailyFileLogger.Warn($"ScanStockOut: EPC {dto.Epc} tidak ditemukan");
                return;
            }

            if (tag.Status != "RESERVED")
            {
                DailyFileLogger.Warn($"ScanStockOut: Tag {tag.TagId} status {tag.Status} bukan RESERVED");
                return;
            }

            var reserved = await _db.TransactionDetails
                .Include(x => x.Transaction)
                .FirstOrDefaultAsync(x =>
                    x.TagId == tag.Id &&
                    x.Transaction.TrsType == "STOCK_PREPARATION" &&
                    x.Transaction.ReferenceId == dto.DoId);

            if (reserved == null)
            {
                DailyFileLogger.Warn($"ScanStockOut: Tag {tag.TagId} tidak termasuk DO {dto.DoId}");
                return;
            }

            tag.Status = "OUT";
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;

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

            _db.TransactionDetails.Add(new Transaction_Detail
            {
                TrdId = Guid.NewGuid().ToString(),
                TrsId = trxHeader.TrsId,
                TagId = tag.Id,
                ItemId = tag.ItemId
            });

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"ScanStockOut berhasil. DO={dto.DoId}, Tag={tag.TagId}, Reader={dto.ReaderId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error di ScanStockOutAsync", ex);
            throw;
        }
    }
}