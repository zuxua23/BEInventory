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

    public async Task<List<object>> GetSystemDataAsync(string sttId)
    {
        var data = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == "SYSTEM")
            .Join(_db.Tags,
                std => std.TagId,
                tag => tag.Id,
                (std, tag) => new { std.ItemId, tag.LocationId })
            .Join(_db.Locations,
                x => x.LocationId,
                loc => loc.Id,
                (x, loc) => new { x.ItemId, LocationName = loc.Name })
            .GroupBy(x => new { x.ItemId, x.LocationName })
            .Select(g => new
            {
                ItemId = g.Key.ItemId,
                Location = g.Key.LocationName,
                Qty = g.Count()
            })
            .ToListAsync<object>();

        return data;
    }

    public async Task<string> CreateAsync(StockTakingCreateDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var active = await _db.StockTakings.AnyAsync(x => x.Status == "OPEN");
            if (active) throw new Exception("Masih ada stock taking aktif");

            var sttId = Guid.NewGuid().ToString();

            var st = new StockTaking
            {
                SttId = sttId,
                Remark = dto.Remark,
                Status = "OPEN",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockTakings.Add(st);

            var query = _db.Tags.Where(t => t.Status == "IN_STOCK");

            if (dto.LocationIds != null && dto.LocationIds.Any())
                query = query.Where(t => dto.LocationIds.Contains(t.LocationId));

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
            var result = await _db.Tags.Where(t => t.Status == "IN_STOCK").ToListAsync();
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
            var st = await _db.StockTakings.FirstOrDefaultAsync(x => x.SttId == dto.SttId);
            if (st == null) throw new Exception("Stock taking tidak ditemukan");
            if (st.Status != "OPEN") throw new Exception("Stock taking sudah selesai");

            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.EpcTag == dto.Epc);
            if (tag == null) throw new Exception("Tag tidak ditemukan");

            var existsInSystem = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId && x.TagId == tag.Id && x.Action == "SYSTEM");
            if (!existsInSystem) throw new Exception("Tag tidak termasuk dalam snapshot stock taking");

            var alreadyScanned = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId && x.TagId == tag.Id && x.Action == "FOUND");
            if (alreadyScanned) return;

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
            var st = await _db.StockTakings.FirstOrDefaultAsync(x => x.SttId == dto.SttId);
            if (st == null) throw new Exception("Stock taking tidak ditemukan");
            if (st.Status != "OPEN") throw new Exception("Stock taking sudah selesai");

            var epcs = dto.Items.Select(x => x.Epc).Distinct().ToList();
            var tags = await _db.Tags.Where(t => epcs.Contains(t.EpcTag)).ToListAsync();
            if (!tags.Any()) return;

            var systemTagIds = await _db.StockTakingDetails
                .Where(x => x.SttId == dto.SttId && x.Action == "SYSTEM")
                .Select(x => x.TagId)
                .ToListAsync();

            var existingFound = await _db.StockTakingDetails
                .Where(x => x.SttId == dto.SttId && x.Action == "FOUND")
                .Select(x => x.TagId)
                .ToListAsync();

            var validTags = tags
                .Where(t => systemTagIds.Contains(t.Id) && !existingFound.Contains(t.Id))
                .ToList();

            if (!validTags.Any()) return;

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
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.TagId == dto.TagId);
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
                // TagId sengaja tidak di-set karena ini manual (tidak ada tag fisik)
            });

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"StockTaking manual add. SttId={dto.SttId}, Item={dto.ItemId}, Remark={dto.Remark}");
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
            .Select(g => new { ItemId = g.Key, QtySystem = g.Count() })
            .ToListAsync();

        var scan = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == "FOUND")
            .GroupBy(x => x.ItemId)
            .Select(g => new { ItemId = g.Key, QtyScan = g.Count() })
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
            var st = await _db.StockTakings.FirstOrDefaultAsync(x => x.SttId == dto.SttId);
            if (st == null) throw new Exception("Session tidak ditemukan");
            if (st.Status != "OPEN") throw new Exception("Session sudah ditutup");

            var details = await _db.StockTakingDetails
                .Where(d => d.SttId == dto.SttId)
                .ToListAsync();

            try
            {
                await ApplyAdjustments(details, dto.SttId, user);
            }
            catch (Exception ex)
            {
                DailyFileLogger.Warn($"Adjustment gagal tapi finalize tetap lanjut. Session={dto.SttId} | {ex.Message}");
            }

            st.Status = "COMPLETED";

            _db.Transactions.Add(new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = "STOCK_TAKING_FINALIZE",
                ReferenceId = dto.SttId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"StockTaking finalize sukses. Session={dto.SttId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Finalize error", ex);
            throw;
        }
    }

    public async Task ApplyAdjustmentAsync(string sttId, string user)
    {
        var details = await _db.StockTakingDetails
            .Where(d => d.SttId == sttId)
            .ToListAsync();

        await ApplyAdjustments(details, sttId, user);
        await _db.SaveChangesAsync();
    }

    private async Task ApplyAdjustments(List<StockTakingDetail> details, string sttId, string user)
    {
        // ── REMOVE ────────────────────────────────────────────────
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
                    Reference = sttId,
                    Action = "REMOVE",
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // ── ADD_MANUAL ────────────────────────────────────────────
        // FIX: filter by ItemId != null, bukan TagId != null
        // karena ADD_MANUAL tidak punya TagId (tidak ada tag fisik)
        var addManuals = details
            .Where(d => d.Action == "ADD_MANUAL" && d.ItemId != null)
            .ToList();

        foreach (var add in addManuals)
        {
            // Cari tag STANDBY yang memiliki ItemId sama, belum dipakai
            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.ItemId == add.ItemId && t.Status == "STANDBY");

            if (tag == null)
            {
                DailyFileLogger.Warn($"ADD_MANUAL skip: tidak ada tag STANDBY untuk ItemId={add.ItemId}");
                continue;
            }

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
                Reference = sttId,
                Action = "ADD_MANUAL",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });
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

    public async Task<List<object>> GetSessionTagsAsync(string sttId)
    {
        try
        {
            var data = await _db.StockTakingDetails
                .Where(x => x.SttId == sttId && x.Action == "SYSTEM")
                .Join(_db.Tags,
                    std => std.TagId,
                    tag => tag.Id,
                    (std, tag) => new { std, tag })
                .Join(_db.Items,
                    x => x.tag.ItemId,
                    item => item.Id,
                    (x, item) => new { x.std, x.tag, item })
                .Join(_db.Locations,
                    x => x.tag.LocationId,
                    loc => loc.Id,
                    (x, loc) => (object)new
                    {
                        tagId = x.tag.TagId,
                        epcTag = x.tag.EpcTag,
                        itemId = x.tag.ItemId,
                        itemName = x.item.Name,
                        location = loc.Name
                    })
                .ToListAsync();

            return data;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("GetSessionTagsAsync error", ex);
            throw;
        }
    }

    public async Task<object?> GetActiveAsync()
    {
        var st = await _db.StockTakings
            .Where(x => x.Status == "OPEN")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (st == null) return null;

        var locations = await _db.StockTakingDetails
            .Where(x => x.SttId == st.SttId && x.Action == "SYSTEM" && x.TagId != null)
            .Join(_db.Tags,
                std => std.TagId,
                tag => tag.Id,
                (std, tag) => tag.LocationId)
            .Distinct()
            .Join(_db.Locations,
                locId => locId,
                loc => loc.Id,
                (locId, loc) => loc.Name)
            .ToListAsync();

        return new
        {
            st.SttId,
            st.Remark,
            st.Status,
            Location = locations.Any() ? string.Join(", ", locations) : "-"
        };
    }
}