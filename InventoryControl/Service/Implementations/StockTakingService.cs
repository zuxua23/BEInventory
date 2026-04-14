namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class StockTakingService : IStockTakingService
{
    private readonly AppDBContext _db;

    public StockTakingService(AppDBContext db)
    {
        _db = db;
    }


    public async Task<string> CreateAsync(StockTakingCreateDto dto, string user)
    {

        try
        {
            var st = new StockTaking
            {
                SttId = Guid.NewGuid().ToString(),
                Remark = dto.Remark,
                Status = "OPEN",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockTakings.Add(st);
            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"StockTaking session dibuat. SttId={st.SttId}, User={user}");

            return st.SttId;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di CreateAsync StockTaking", ex);
            throw;
        }
    }

    public async Task<List<Tag>> GetStockDataAsync()
    {
        try
        {
            var result = await _db.Tags
                .Where(t => t.Status == "IN_STOCK")
                .ToListAsync();

            DailyFileLogger.Info($"GetStockDataAsync berhasil. Total tag IN_STOCK: {result.Count}");

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di GetStockDataAsync", ex);
            throw;
        }
    }

    public async Task ScanAsync(StockTakingScanDto dto)
    {
        try
        {
            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.EpcTag == dto.Epc);

            if (tag == null)
            {
                DailyFileLogger.Warn($"ScanAsync gagal: EPC {dto.Epc} tidak ditemukan");
                throw new Exception("Tag tidak ditemukan");
            }

            var exists = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId && x.TagId == tag.Id);

            if (exists)
                return;

            _db.StockTakingDetails.Add(new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                TagId = tag.Id,
                Action = "FOUND"
            });

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"StockTaking scan berhasil. SttId={dto.SttId}, Tag={tag.TagId}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di ScanAsync StockTaking", ex);
            throw;
        }
    }

    public async Task RemoveAsync(StockTakingRemoveDto dto)
    {
        try
        {
            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.TagId == dto.TagId);

            if (tag == null)
            {
                DailyFileLogger.Warn($"RemoveAsync gagal: Tag {dto.TagId} tidak ditemukan");
                throw new Exception("Tag tidak ditemukan");
            }

            _db.StockTakingDetails.Add(new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                TagId = tag.Id,
                Action = "REMOVE"
            });

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"StockTaking remove dicatat. SttId={dto.SttId}, Tag={dto.TagId}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di RemoveAsync StockTaking", ex);
            throw;
        }
    }

    public async Task ManualAddAsync(StockTakingManualAddDto dto)
    {
        try
        {
            _db.StockTakingDetails.Add(new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                ItemId = dto.ItemId,
                Remark = dto.Remark,
                Action = "ADD_MANUAL"
            });

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"StockTaking manual add. SttId={dto.SttId}, Item={dto.ItemId}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di ManualAddAsync StockTaking", ex);
            throw;
        }
    }

    public async Task FinalizeAsync(StockTakingFinalizeDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var st = await _db.StockTakings
                .FirstOrDefaultAsync(x => x.SttId == dto.SttId && x.Status == "OPEN");

            if (st == null)
            {
                DailyFileLogger.Warn($"FinalizeAsync gagal: Session {dto.SttId} tidak aktif");
                throw new Exception("Session tidak aktif");
            }

            var details = await _db.StockTakingDetails
                .Where(d => d.SttId == dto.SttId)
                .ToListAsync();


            var removeTagIds = details
                .Where(d => d.Action == "REMOVE" && d.TagId != null)
                .Select(d => d.TagId)
                .Distinct()
                .ToList();

            var removeTags = await _db.Tags
                .Where(t => removeTagIds.Contains(t.Id))
                .ToListAsync();

            foreach (var tag in removeTags)
            {
                if (tag.Status == "IN_STOCK")
                {
                    tag.Status = "OUT";
                    tag.UpdatedBy = user;
                    tag.UpdatedAt = DateTime.UtcNow;

                    _db.Histories.Add(new HistoryPrint
                    {
                        Id = Guid.NewGuid().ToString(),
                        TagId = tag.Id,
                        ItemId = tag.ItemId,
                        Type = "STOCK_ADJUSTMENT",
                        Reference = dto.SttId,
                        Action = "REMOVE",
                        CreatedBy = user,
                        CreatedAt = DateTime.UtcNow
                    });

                    DailyFileLogger.Info($"StockAdjustment REMOVE. Tag={tag.TagId}, Session={dto.SttId}");
                }
            }

            var addManuals = details
                .Where(d => d.Action == "ADD_MANUAL" && d.TagId != null)
                .ToList();

            // Validasi duplicate tag
            var duplicate = addManuals
                .GroupBy(x => x.TagId)
                .Where(g => g.Count() > 1)
                .Any();

            if (duplicate)
                throw new Exception("Ada tag yang dipakai lebih dari sekali di manual add");

            var addTagIds = addManuals
                .Select(x => x.TagId)
                .Distinct()
                .ToList();

            var addTags = await _db.Tags
                .Where(t => addTagIds.Contains(t.Id))
                .ToListAsync();

            foreach (var add in addManuals)
            {
                var tag = addTags.FirstOrDefault(t => t.Id == add.TagId);

                if (tag == null)
                    throw new Exception($"Tag {add.TagId} tidak ditemukan");

                if (tag.Status != "STANDBY")
                    throw new Exception($"Tag {tag.TagId} tidak dalam kondisi STANDBY");

                tag.Status = "IN_STOCK";
                tag.ItemId = add.ItemId;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.Histories.Add(new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = add.ItemId,
                    Type = "STOCK_ADJUSTMENT",
                    Reference = dto.SttId,
                    Action = "ADD_MANUAL",
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });

                DailyFileLogger.Info($"StockAdjustment ADD_MANUAL. Tag={tag.TagId}, Item={add.ItemId}");
            }

            _db.Transactions.Add(new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = "STOCK_ADJUSTMENT",
                ReferenceId = dto.SttId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });

            st.Status = "COMPLETED";

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"StockTaking finalize berhasil. Session={dto.SttId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error di FinalizeAsync StockTaking", ex);
            throw;
        }
    }
}