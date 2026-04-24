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
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var sttId = Guid.NewGuid().ToString();

            var st = new StockTaking
            {
                SttId = sttId,
                Remark = dto.Remark,
                LocationId = dto.LocationId ?? "ALL",
                Status = "OPEN",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockTakings.Add(st);

            var query = _db.Tags.Where(t => t.Status == "IN_STOCK");

            if (st.LocationId != "ALL")
            {
                query = query.Where(t => t.LocationId == st.LocationId);
            }

            var tags = await query.ToListAsync();

            var snapshot = tags.Select(t => new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = sttId,
                TagId = t.Id,
                ItemId = t.ItemId,
                Action = "SYSTEM" 
            });

            await _db.StockTakingDetails.AddRangeAsync(snapshot);

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"StockTaking SNAPSHOT created. Count={tags.Count}");

            return sttId;
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("CreateAsync Snapshot error", ex);
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
            var st = await _db.StockTakings
                .FirstOrDefaultAsync(x => x.SttId == dto.SttId);

            if (st == null)
                throw new Exception("Stock taking tidak ditemukan");

            if (st.Status != "OPEN")
                throw new Exception("Stock taking sudah selesai");

            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.EpcTag == dto.Epc);

            if (tag == null)
                throw new Exception("Tag tidak ditemukan");

            if (st.LocationId != "ALL" && tag.LocationId != st.LocationId)
                throw new Exception("Tag tidak sesuai lokasi stock taking");

            var existsInSystem = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId
                            && x.TagId == tag.Id
                            && x.Action == "SYSTEM");

            if (!existsInSystem)
                throw new Exception("Tag tidak termasuk dalam snapshot stock taking");

            var alreadyScanned = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId
                            && x.TagId == tag.Id
                            && x.Action == "FOUND");

            if (alreadyScanned)
                return; 

            _db.StockTakingDetails.Add(new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                TagId = tag.Id,
                ItemId = tag.ItemId,
                Action = "FOUND"
            });

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"Scan success. SttId={dto.SttId}, Tag={tag.TagId}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("ScanAsync error", ex);
            throw;
        }
    }
    public async Task BulkScanAsync(StockTakingBulkScanDto dto)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var st = await _db.StockTakings
                .FirstOrDefaultAsync(x => x.SttId == dto.SttId);

            if (st == null)
                throw new Exception("Stock taking tidak ditemukan");

            if (st.Status != "OPEN")
                throw new Exception("Stock taking sudah selesai");

            var epcs = dto.Items.Select(x => x.Epc).Distinct().ToList();

            var tags = await _db.Tags
                .Where(t => epcs.Contains(t.EpcTag))
                .ToListAsync();

            if (!tags.Any())
                return;

            var systemTagIds = await _db.StockTakingDetails
                .Where(x => x.SttId == dto.SttId && x.Action == "SYSTEM")
                .Select(x => x.TagId)
                .ToListAsync();

            var existingFound = await _db.StockTakingDetails
                .Where(x => x.SttId == dto.SttId && x.Action == "FOUND")
                .Select(x => x.TagId)
                .ToListAsync();

            var validTags = tags
                .Where(t =>
                    (st.LocationId == "ALL" || t.LocationId == st.LocationId) && // lokasi
                    systemTagIds.Contains(t.Id) && 
                    !existingFound.Contains(t.Id)  
                )
                .ToList();

            if (!validTags.Any())
                return;

            var newData = validTags.Select(t => new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                TagId = t.Id,
                ItemId = t.ItemId,
                Action = "FOUND"
            });

            await _db.StockTakingDetails.AddRangeAsync(newData);
            await _db.SaveChangesAsync();

            await trx.CommitAsync();

            DailyFileLogger.Info($"Bulk scan success. Count={validTags.Count}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("BulkScanAsync error", ex);
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
    public async Task<object> GetCompareAsync(string sttId)
    {
        var system = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == "SYSTEM")
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                QtySystem = g.Count()
            })
            .ToListAsync();

        var scan = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == "FOUND")
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                QtyScan = g.Count()
            })
            .ToListAsync();

        return system.Select(s => new
        {
            s.ItemId,
            s.QtySystem,
            QtyScan = scan.FirstOrDefault(x => x.ItemId == s.ItemId)?.QtyScan ?? 0,
            Status = scan.Any(x => x.ItemId == s.ItemId) ? "Scanned" : "Pending",
            Selisih = (scan.FirstOrDefault(x => x.ItemId == s.ItemId)?.QtyScan ?? 0) - s.QtySystem
        });
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

    public async Task<object> GetProgressAsync(string sttId)
    {
        var total = await _db.StockTakingDetails
            .CountAsync(x => x.SttId == sttId && x.Action == "SYSTEM");

        var scanned = await _db.StockTakingDetails
            .CountAsync(x => x.SttId == sttId && x.Action == "FOUND");

        return new
        {
            Total = total,
            Scanned = scanned,
            Progress = total == 0 ? 0 : (scanned * 100 / total)
        };
    }
}