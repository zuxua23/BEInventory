namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

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
        var tag = await _db.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.EpcTag == dto.Epc);

        if (tag == null || tag.Status != "RESERVED")
            return;

        var isValid = await _db.TransactionDetails
            .AsNoTracking()
            .AnyAsync(x =>
                x.TagId == tag.Id &&
                x.Transaction.TrsType == "STOCK_PREPARATION" &&
                x.Transaction.ReferenceId == dto.DoId);

        if (!isValid) return;

        var trx = await _db.Transactions
            .FirstOrDefaultAsync(x =>
                x.TrsType == "STOCK_OUT" &&
                x.ReferenceId == dto.DoId);

        if (trx == null)
        {
            trx = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = "STOCK_OUT",
                ReferenceId = dto.DoId,
                ReaderId = dto.ReaderId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(trx);
            await _db.SaveChangesAsync();
        }

        var exists = await _db.TransactionDetails
            .AnyAsync(x => x.TagId == tag.Id && x.TrsId == trx.TrsId);

        if (exists) return;

        var tagUpdate = new Tag
        {
            Id = tag.Id,
            Status = "OUT",
            UpdatedBy = user,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tags.Attach(tagUpdate);
        _db.Entry(tagUpdate).Property(x => x.Status).IsModified = true;

        _db.TransactionDetails.Add(new Transaction_Detail
        {
            TrdId = Guid.NewGuid().ToString(),
            TrsId = trx.TrsId,
            TagId = tag.Id,
            ItemId = tag.ItemId
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<ItemListDto>> GetItemsAsync(string doId)
    {
        try
        {
            var items = await (
                  from d in _db.DODetails
                  join i in _db.Items on d.ItemId equals i.Id
                  where d.DoId == doId

                  select new ItemListDto
                  {
                      ItemId = d.ItemId,
                      ItemCode = i.ItmId,
                      ItemName = i.Name,
                      Required = d.QtyRequired ?? 0,

                      Reserved = _db.TransactionDetails
                .Count(td => td.ItemId == d.ItemId &&
                             td.Transaction.ReferenceId == doId &&
                             td.Transaction.TrsType == "STOCK_PREPARATION"),

                      Scanned = _db.TransactionDetails
                .Count(td => td.ItemId == d.ItemId &&
                             td.Transaction.ReferenceId == doId &&
                             td.Transaction.TrsType == "STOCK_OUT")
                  })
            .ToListAsync();

            if (items != null)
                DailyFileLogger.Info($"GetByIdAsync Item berhasil untuk ID {doId}.");
            else
                DailyFileLogger.Warn($"GetByIdAsync Item: ID {doId} tidak ditemukan.");

            return items;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di GetItems untuk DO ID {doId}.", ex);
            throw;

        }
    }
    public async Task<ProgressDto> GetProgressAsync(string doId)
    {
        var total = await _db.TransactionDetails
            .CountAsync(x => x.Transaction.ReferenceId == doId);

        var scanned = await _db.TransactionDetails
            .CountAsync(x => x.Transaction.ReferenceId == doId &&
                             x.Transaction.TrsType == "STOCK_OUT");

        return new ProgressDto
        {
            Total = total,
            Scanned = scanned
        };
    }
    public async Task<List<TagDto>> GetTagsAsync(string doId)
    {
        var tags = await _db.TransactionDetails
            .Where(x => x.Transaction.ReferenceId == doId &&
                        x.Transaction.TrsType == "STOCK_OUT")
            .Select(x => new TagDto
            {
                TagId = x.TagId,
                ItemId = x.ItemId
            })
            .ToListAsync();

        return tags;
    }
    public static class RfidSession
    {
        private static ConcurrentDictionary<string, string> _sessions = new();

        public static void Set(string readerId, string doId)
        {
            _sessions[readerId] = doId;
        }

        public static string Get(string readerId)
        {
            return _sessions.ContainsKey(readerId) ? _sessions[readerId] : null;
        }

        public static void Remove(string readerId)
        {
            _sessions.TryRemove(readerId, out _);
        }
    }
}