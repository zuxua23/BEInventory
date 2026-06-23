namespace InventoryControl.Service.Implementations;

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;
using System.Text;

public class StockTakingService : IStockTakingService
{
    private readonly AppDBContext _db;

    public StockTakingService(AppDBContext db)
    {
        _db = db;
    }
    public async Task<List<Location>> GetLocAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving active locations with IN_STOCK tags."
            );

            var result = await _db.Locations
                .Where(location =>
                    !location.IsDelete &&
                    _db.Tags.Any(tag =>
                        tag.LocationId == location.Id &&
                        tag.Status == TagStatus.IN_STOCK
                    )
                )
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} location(s) containing IN_STOCK tags."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving locations with IN_STOCK tags.",
                ex
            );

            throw;
        }
    }

    public async Task<object?> GetActiveAsync()
    {
        var session = await _db.StockTakings
            .Where(x => x.Status == TakingStatus.OPEN)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (session == null)
            return null;

        var locations = await _db.StockTakingDetails
            .Where(x =>
                x.SttId == session.SttId &&
                x.Action == TakingAction.SYSTEM)
            .Join(_db.Tags,
                std => std.TagId,
                tag => tag.Id,
                (std, tag) => tag.LocationId)
            .Distinct()
            .Join(_db.Locations,
                locId => locId,
                loc => loc.Id,
                (locId, loc) => new { Id = locId, Name = loc.Name })
            .ToListAsync();

        var locationIds = locations.Select(x => x.Id).ToList();
        var locationNames = locations.Select(x => x.Name).ToList();

        return new
        {
            session.SttId,
            session.Remark,
            session.Status,
            LocationIds = locationIds,
            Locations = locationNames,
            Location = string.Join(", ", locationNames),
            CreatedAt = session.CreatedAt.HasValue
                ? session.CreatedAt.Value.ToString("dd/MM/yyyy")
                : "-"
        };
    }

    public async Task<List<StockTakingSessionTagDto>> GetSessionTagsAsync(string sttId)
    {
        var data = await (from std in _db.StockTakingDetails
                          where std.SttId == sttId && std.Action == TakingAction.SYSTEM
                          join tag in _db.Tags on std.TagId equals tag.Id
                          join item in _db.Items on tag.ItemId equals item.Id
                          join loc in _db.Locations on tag.LocationId equals loc.Id
                          let latestAction = _db.StockTakingDetails
                              .Where(x => x.SttId == sttId && x.TagId == tag.Id && x.Action != TakingAction.SYSTEM)
                              .Select(x => x.Action.ToString())
                              .FirstOrDefault()
                          select new StockTakingSessionTagDto
                          {
                              TagId = tag.TagId,
                              EpcTag = tag.EpcTag,
                              ItemId = tag.ItemId,
                              ItemCode = item.ItmId,
                              ItemName = item.Name,
                              LocationId = loc.Id,
                              Location = loc.Name,
                              Action = latestAction ?? "SYSTEM"
                          }).ToListAsync();

        DailyFileLogger.Info($"GetSessionTagsAsync SttId={sttId} count={data.Count}");

        return data;
    }

    public async Task<List<object>> GetSystemDataAsync(string sttId)
    {
        var data = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == TakingAction.SYSTEM)
            .Join(_db.Tags,
                std => std.TagId,
                tag => tag.Id,
                (std, tag) => new
                {
                    std.ItemId,
                    tag.LocationId
                })
            .Join(_db.Locations,
                x => x.LocationId,
                loc => loc.Id,
                (x, loc) => new
                {
                    x.ItemId,
                    LocationName = loc.Name
                })
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
            var active = await _db.StockTakings
    .AnyAsync(x => x.Status == TakingStatus.OPEN);

            if (active)
                throw new Exception("There is still an active stock taking session");
            var sttId = Guid.NewGuid().ToString();

            var st = new StockTaking
            {
                SttId = sttId,
                Remark = dto.Remark,
                Status = TakingStatus.OPEN,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockTakings.Add(st);

            var query = _db.Tags.Where(t => t.Status == TagStatus.IN_STOCK);

            if (dto.LocationIds != null && dto.LocationIds.Any())
            {
                var locationIds = dto.LocationIds;
                query = query.Where(t => EF.Constant(locationIds).Contains(t.LocationId));
            }

            var tags = await query.ToListAsync();

            var snapshot = tags.Select(t => new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = sttId,
                TagId = t.Id,
                ItemId = t.ItemId,
                Action = TakingAction.SYSTEM
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
                .Where(t => t.Status == TagStatus.IN_STOCK)
                .ToListAsync();

            DailyFileLogger.Info($"GetStockDataAsync completed successfully. Total IN_STOCK tags: {result.Count}");

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("An error occurred in GetStockDataAsync", ex);
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
                throw new Exception("Stock taking session was not found");

            if (st.Status != TakingStatus.OPEN)
                throw new Exception("Stock taking session has already been completed");

            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.EpcTag == dto.Epc);

            if (tag == null)
                throw new Exception("Tag was not found");

            var existsInSystem = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId
                            && x.TagId == tag.Id
                            && x.Action == TakingAction.SYSTEM);

            if (!existsInSystem)
                throw new Exception("Tag is not included in the stock taking snapshot");

            var alreadyScanned = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId
                            && x.TagId == tag.Id
                            && x.Action == TakingAction.FOUND);

            if (alreadyScanned)
                return;

            _db.StockTakingDetails.Add(new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                TagId = tag.Id,
                ItemId = tag.ItemId,
                Action = TakingAction.FOUND
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
                throw new Exception("Stock taking session was not found");

            if (st.Status != TakingStatus.OPEN)
                throw new Exception("Stock taking session has already been completed");

            var epcs = dto.Items.Select(x => x.Epc).Distinct().ToList();

            var tags = await _db.Tags
                .Where(t => EF.Constant(epcs).Contains(t.EpcTag))
                .ToListAsync();

            if (!tags.Any())
                return;

            var systemTagIds = await _db.StockTakingDetails
                .Where(x => x.SttId == dto.SttId && x.Action == TakingAction.SYSTEM)
                .Select(x => x.TagId)
                .ToListAsync();

            var existingFound = await _db.StockTakingDetails
                .Where(x => x.SttId == dto.SttId && x.Action == TakingAction.FOUND)
                .Select(x => x.TagId)
                .ToListAsync();

            var validTags = tags
                .Where(t =>
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
                Action = TakingAction.FOUND
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
                DailyFileLogger.Warn($"RemoveAsync failed: Tag {dto.TagId} was not found");
                throw new Exception("Tag was not found");
            }
            var existsInSystem = await _db.StockTakingDetails
                .AnyAsync(x =>
                    x.SttId == dto.SttId &&
                    x.TagId == tag.Id &&
                    x.Action == TakingAction.SYSTEM
                );

            if (!existsInSystem)
                throw new Exception("Tag is not included in this stock taking snapshot");

            _db.StockTakingDetails.Add(new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                TagId = tag.Id,
                Action = TakingAction.REMOVE
            });

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"StockTaking remove has been recorded. SttId={dto.SttId}, Tag={dto.TagId}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error in RemoveAsync StockTaking", ex);
            throw;
        }
    }
    public async Task<ValidateManualTagResultDto> ValidateManualTagAsync(string epcTag, string sttId)
    {
        var tag = await _db.Tags
            .Include(t => t.Item)
            .FirstOrDefaultAsync(t => t.EpcTag == epcTag || t.TagId == epcTag);

        if (tag == null) return null;

        if (tag.Status != TagStatus.OUT &&
            tag.Status != TagStatus.PRINTED &&
            tag.Status != TagStatus.STANDBY) return null;

        var alreadyUsed = await _db.StockTakingDetails
            .AnyAsync(x => x.SttId == sttId && x.TagId == tag.Id);

        if (alreadyUsed) return null;

        return new ValidateManualTagResultDto
        {
            TagId = tag.TagId,
            EpcTag = tag.EpcTag,
            Status = tag.Status.ToString(),
            ItemId = tag.ItemId,
            ItemName = tag.Item?.Name
        };
    }

    public async Task ManualAddAsync(StockTakingManualAddDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var st = await _db.StockTakings
                .FirstOrDefaultAsync(x => x.SttId == dto.SttId);

            if (st == null)
                throw new Exception("Stock taking session tidak ditemukan");

            if (st.Status != TakingStatus.OPEN)
                throw new Exception("Stock taking session sudah selesai");

            var tag = await _db.Tags
                .FirstOrDefaultAsync(x =>
                    x.EpcTag == dto.NewTagId ||
                    x.TagId == dto.NewTagId
                );

            if (tag == null)
                throw new Exception("Tag tidak ditemukan");

            if (tag.Status != TagStatus.STANDBY &&
                tag.Status != TagStatus.PRINTED &&
                tag.Status != TagStatus.OUT)
                throw new Exception($"Status tag '{tag.Status}' tidak eligible. Harus STANDBY, PRINTED, atau OUT");

            var alreadyUsed = await _db.StockTakingDetails
                .AnyAsync(x => x.SttId == dto.SttId && x.TagId == tag.Id);

            if (alreadyUsed)
                throw new Exception("Tag sudah digunakan dalam sesi stock taking ini");

            var sessionLocationId = await _db.StockTakingDetails
                .Where(x => x.SttId == dto.SttId && x.Action == TakingAction.SYSTEM)
                .Join(_db.Tags,
                    std => std.TagId,
                    t => t.Id,
                    (std, t) => t.LocationId)
                .FirstOrDefaultAsync();

            tag.Status = TagStatus.IN_STOCK;
            tag.ItemId = !string.IsNullOrEmpty(dto.ItemId) ? dto.ItemId : tag.ItemId;
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;
            if (sessionLocationId != null)
                tag.LocationId = sessionLocationId;

            _db.StockTakingDetails.Add(new StockTakingDetail
            {
                StdId = Guid.NewGuid().ToString(),
                SttId = dto.SttId,
                TagId = tag.Id,
                ItemId = tag.ItemId,
                Remark = dto.Remark,
                Action = TakingAction.ADD_MANUAL
            });

            _db.Histories.Add(new HistoryPrint
            {
                Id = Guid.NewGuid().ToString(),
                TagId = tag.Id,
                ItemId = tag.ItemId,
                Type = HistoryType.STOCK_ADJUSTMENT,
                Reference = dto.SttId,
                Action = "ADD_MANUAL",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info(
                $"ManualAdd success. SttId={dto.SttId}, Tag={tag.TagId}, EPC={tag.EpcTag}, " +
                $"Status→IN_STOCK, Location={tag.LocationId}, User={user}"
            );
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("ManualAddAsync error", ex);
            throw;
        }
    }


    public async Task<object> GetCompareAsync(string sttId)
    {
        var system = await _db.StockTakingDetails
             .Where(x =>
                 x.SttId == sttId &&
                 x.Action == TakingAction.SYSTEM)
             .Join(_db.Tags,
                 std => std.TagId,
                 tag => tag.Id,
                 (std, tag) => new
                 {
                     std.ItemId,
                     tag.LocationId
                 })
             .Join(_db.Locations,
                 x => x.LocationId,
                 loc => loc.Id,
                 (x, loc) => new
                 {
                     x.ItemId,
                     Location = loc.Name
                 })
             .GroupBy(x => new
             {
                 x.ItemId,
                 x.Location
             })
             .Select(g => new
             {
                 ItemId = g.Key.ItemId,
                 Location = g.Key.Location,
                 QtySystem = g.Count()
             })
             .ToListAsync();

        var scan = await _db.StockTakingDetails
                .Where(x =>
                    x.SttId == sttId &&
                    x.Action == TakingAction.FOUND)
                .Join(_db.Tags,
                    std => std.TagId,
                    tag => tag.Id,
                    (std, tag) => new
                    {
                        std.ItemId,
                        tag.LocationId
                    })
                .Join(_db.Locations,
                    x => x.LocationId,
                    loc => loc.Id,
                    (x, loc) => new
                    {
                        x.ItemId,
                        Location = loc.Name
                    })
                .GroupBy(x => new
                {
                    x.ItemId,
                    x.Location
                })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    Location = g.Key.Location,
                    QtyScan = g.Count()
                })
                .ToListAsync();

        return system.Select(s =>
        {
            var scanned = scan.FirstOrDefault(x =>
                x.ItemId == s.ItemId &&
                x.Location == s.Location
            );

            var qtyScan = scanned?.QtyScan ?? 0;
            var difference = qtyScan - s.QtySystem;

            return new
            {
                s.ItemId,
                s.Location,
                s.QtySystem,
                QtyScan = qtyScan,
                Difference = difference,
                Status =
                    difference == 0
                        ? "MATCH"
                        : "MISSING"
            };
        });
    }
    public async Task FinalizeAsync(StockTakingFinalizeDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var st = await _db.StockTakings
                .FirstOrDefaultAsync(x => x.SttId == dto.SttId);

            if (st == null)
                throw new Exception("Stock taking session was not found");

            if (st.Status != TakingStatus.OPEN)
                throw new Exception("Stock taking session has already been completed");

            var details = await _db.StockTakingDetails
                .Where(d => d.SttId == dto.SttId)
                .ToListAsync();

            await ApplyAdjustments(details, dto.SttId, user);

            st.Status = TakingStatus.COMPLETED;

            _db.Transactions.Add(new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = TransactionType.STOCK_TAKING_FINALIZE,
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

    private async Task ApplyAdjustments(List<StockTakingDetail> details, string sttId, string user)
    {
        var removeTagIds = details
            .Where(d => d.Action == TakingAction.REMOVE && d.TagId != null)
            .Select(d => d.TagId)
            .Distinct()
            .ToList();

        var removeTags = await _db.Tags
            .Where(t => EF.Constant(removeTagIds).Contains(t.Id))
            .ToListAsync();

        foreach (var tag in removeTags)
        {
            if (tag.Status == TagStatus.IN_STOCK)
            {
                tag.Status = TagStatus.OUT;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.Histories.Add(new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = tag.ItemId,
                    Type = HistoryType.STOCK_ADJUSTMENT,
                    Reference = sttId,
                    Action = "REMOVE",
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var addManuals = details
            .Where(d => d.Action == TakingAction.ADD_MANUAL && d.TagId != null)
            .ToList();

        var addTagIds = addManuals.Select(x => x.TagId).Distinct().ToList();

        var addTags = await _db.Tags
            .Where(t => EF.Constant(addTagIds).Contains(t.Id))
            .ToListAsync();

        var systemDetails = details
            .Where(d => d.Action == TakingAction.SYSTEM && d.TagId != null)
            .ToList();

        var systemTagIds = systemDetails.Select(d => d.TagId!).Distinct().ToList();
        var systemTagLocations = await _db.Tags
            .Where(t => EF.Constant(systemTagIds).Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.LocationId);

        foreach (var add in addManuals)
        {
            var tag = addTags.FirstOrDefault(t => t.Id == add.TagId);

            if (tag == null) continue;

            if (tag.Status != TagStatus.STANDBY &&
                tag.Status != TagStatus.PRINTED &&
                tag.Status != TagStatus.OUT &&
                tag.Status != TagStatus.IN_STOCK) continue;

            tag.Status = TagStatus.IN_STOCK;
            tag.ItemId = add.ItemId;
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;
            tag.UpdatedAt = DateTime.UtcNow;

            var systemDetailForItem = systemDetails.FirstOrDefault(d => d.ItemId == add.ItemId);
            if (systemDetailForItem?.TagId != null &&
                systemTagLocations.TryGetValue(systemDetailForItem.TagId, out var locationId))
            {
                tag.LocationId = locationId;
            }

            _db.Histories.Add(new HistoryPrint
            {
                Id = Guid.NewGuid().ToString(),
                TagId = tag.Id,
                ItemId = add.ItemId,
                Type = HistoryType.STOCK_ADJUSTMENT,
                Reference = sttId,
                Action = "ADD_MANUAL",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<List<StockTakingExportDto>> GetSystemExportData(string sttId)
    {
        return await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == TakingAction.SYSTEM)

            .Join(_db.Tags,
                std => std.TagId,
                tag => tag.Id,
                (std, tag) => new
                {
                    std.ItemId,
                    tag.LocationId
                })

            .Join(_db.Locations,
                x => x.LocationId,
                loc => loc.Id,
                (x, loc) => new
                {
                    x.ItemId,
                    LocationName = loc.Name
                })

            .GroupBy(x => new
            {
                x.ItemId,
                x.LocationName
            })

            .Select(g => new StockTakingExportDto
            {
                ItemId = g.Key.ItemId,
                Location = g.Key.LocationName,
                Qty = g.Count(),
                Status = "SYSTEM"
            })

            .ToListAsync();
    }

    private byte[] GenerateExcel(List<StockTakingExportDto> data)
    {
        using var workbook = new XLWorkbook();

        var ws = workbook.Worksheets.Add("StockTaking");

        ws.Cell(1, 1).Value = "Item";
        ws.Cell(1, 2).Value = "Qty";
        ws.Cell(1, 3).Value = "Location";
        ws.Cell(1, 4).Value = "Status";

        var headerRange = ws.Range(1, 1, 1, 4);

        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 2;

        foreach (var item in data)
        {
            ws.Cell(row, 1).Value = item.ItemId;
            ws.Cell(row, 2).Value = item.Qty;
            ws.Cell(row, 3).Value = item.Location;
            ws.Cell(row, 4).Value = item.Status;

            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private string GenerateCsv(List<StockTakingExportDto> data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Item,Qty,Location,Status");

        foreach (var item in data)
        {
            sb.AppendLine(
                $"{item.ItemId}," +
                $"{item.Qty}," +
                $"{item.Location}," +
                $"{item.Status}"
            );
        }

        return sb.ToString();
    }

    private byte[] GenerateCompareExcel(List<StockTakingCompareExportDto> data)
    {
        using var workbook = new XLWorkbook();

        var ws = workbook.Worksheets.Add("Compare");

        ws.Cell(1, 1).Value = "Item";
        ws.Cell(1, 2).Value = "Location";
        ws.Cell(1, 3).Value = "System Qty";
        ws.Cell(1, 4).Value = "Scan Qty";
        ws.Cell(1, 5).Value = "Difference";
        ws.Cell(1, 6).Value = "Status";

        var header = ws.Range(1, 1, 1, 6);

        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor =
            XLColor.LightGray;

        int row = 2;

        foreach (var item in data)
        {
            ws.Cell(row, 1).Value = item.ItemId;
            ws.Cell(row, 2).Value = item.Location;
            ws.Cell(row, 3).Value = item.QtySystem;
            ws.Cell(row, 4).Value = item.QtyScan;
            ws.Cell(row, 5).Value = item.Difference;
            ws.Cell(row, 6).Value = item.Status;

            var range = ws.Range(row, 1, row, 6);

            if (item.Status == "MATCH")
            {
                range.Style.Fill.BackgroundColor =
                    XLColor.LightGreen;
            }
            else if (item.Status == "MISSING")
            {
                range.Style.Fill.BackgroundColor =
                    XLColor.LightPink;
            }

            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }
    private async Task<List<StockTakingCompareExportDto>> GetCompareExportData(string sttId)
    {
        var systemData = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == TakingAction.SYSTEM)
            .Join(_db.Tags,
                std => std.TagId,
                tag => tag.Id,
                (std, tag) => new
                {
                    std.ItemId,
                    tag.LocationId
                })
            .Join(_db.Locations,
                x => x.LocationId,
                loc => loc.Id,
                (x, loc) => new
                {
                    x.ItemId,
                    Location = loc.Name
                })
            .GroupBy(x => new
            {
                x.ItemId,
                x.Location
            })
            .Select(g => new
            {
                ItemId = g.Key.ItemId,
                Location = g.Key.Location,
                QtySystem = g.Count()
            })
            .ToListAsync();

        var scanData = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == TakingAction.FOUND)
            .Join(_db.Tags,
                std => std.TagId,
                tag => tag.Id,
                (std, tag) => new
                {
                    std.ItemId,
                    tag.LocationId
                })
            .Join(_db.Locations,
                x => x.LocationId,
                loc => loc.Id,
                (x, loc) => new
                {
                    x.ItemId,
                    Location = loc.Name
                })
            .GroupBy(x => new
            {
                x.ItemId,
                x.Location
            })
            .Select(g => new
            {
                ItemId = g.Key.ItemId,
                Location = g.Key.Location,
                QtyScan = g.Count()
            })
            .ToListAsync();

        return systemData.Select(s =>
        {
            var scanned = scanData.FirstOrDefault(x =>
                x.ItemId == s.ItemId &&
                x.Location == s.Location
            );

            var qtyScan = scanned?.QtyScan ?? 0;
            var difference = qtyScan - s.QtySystem;

            return new StockTakingCompareExportDto
            {
                ItemId = s.ItemId,
                Location = s.Location,
                QtySystem = s.QtySystem,
                QtyScan = qtyScan,
                Difference = difference,
                Status =
                    difference == 0
                        ? "MATCH"
                        : "MISSING"
            };
        }).ToList();
    }

    private string GenerateCompareCsv(
    List<StockTakingCompareExportDto> data)
    {
        var sb = new StringBuilder();

        sb.AppendLine(
            "Item,Location,System Qty,Scan Qty,Difference,Status"
        );

        foreach (var item in data)
        {
            sb.AppendLine(
                $"{item.ItemId}," +
                $"{item.Location}," +
                $"{item.QtySystem}," +
                $"{item.QtyScan}," +
                $"{item.Difference}," +
                $"{item.Status}"
            );
        }

        return sb.ToString();
    }

    public async Task<byte[]> ExportSystemExcelAsync(string sttId)
    {
        var data = await GetSystemExportData(sttId);
        if (!data.Any())
            throw new Exception("No system data available to export");

        return GenerateExcel(data);
    }


    public async Task<string> ExportSystemCsvAsync(string sttId)
    {
        var data = await GetSystemExportData(sttId);
        if (!data.Any())
            throw new Exception("No system data available to export");

        return GenerateCsv(data);
    }

    public async Task<byte[]> ExportCompareExcelAsync(string sttId)
    {
        var data = await GetCompareExportData(sttId);

        if (!data.Any())
            throw new Exception("No compare data to export");

        return GenerateCompareExcel(data);
    }
    public async Task<string> ExportCompareCsvAsync(string sttId)
    {
        var data = await GetCompareExportData(sttId);

        if (!data.Any())
            throw new Exception("No compare data to export");

        return GenerateCompareCsv(data);
    }
    public async Task<object> GetProgressAsync(string sttId)
    {
        var total = await _db.StockTakingDetails
            .CountAsync(x =>
                x.SttId == sttId &&
                x.Action == TakingAction.SYSTEM);

        var processed = await _db.StockTakingDetails
            .CountAsync(x =>
                x.SttId == sttId &&
                (
                    x.Action == TakingAction.FOUND ||
                    x.Action == TakingAction.REMOVE ||
                    x.Action == TakingAction.ADD_MANUAL
                ));

        return new
        {
            Total = total,
            Processed = processed,
            Remaining = total - processed,
            Progress =
                total == 0
                    ? 0
                    : (processed * 100 / total)
        };
    }

    public async Task<List<AvailableTagDto>> GetAvailableTagsAsync(string sttId)
    {
        var sessionItemIds = await _db.StockTakingDetails
            .Where(x => x.SttId == sttId && x.Action == TakingAction.SYSTEM)
            .Select(x => x.ItemId)
            .Distinct()
            .ToListAsync();

        var tags = await _db.Tags
            .Where(t =>
                (t.Status == TagStatus.STANDBY || t.Status == TagStatus.PRINTED || t.Status == TagStatus.OUT) &&
                !t.IsDelete &&
                EF.Constant(sessionItemIds).Contains(t.ItemId))
            .Join(_db.Items,
                t => t.ItemId,
                i => i.Id,
                (t, i) => new AvailableTagDto
                {
                    TagId = t.TagId,
                    EpcTag = t.EpcTag,
                    ItemId = t.ItemId,
                    ItemName = i.Name,
                    Status = t.Status.ToString()
                })
            .ToListAsync();

        DailyFileLogger.Info($"GetAvailableTagsAsync SttId={sttId} count={tags.Count}");

        return tags;
    }
    public async Task OperatorSubmitAsync(StockTakingOperatorSubmitDto dto, string updatedBy)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var st = await _db.StockTakings
                .FirstOrDefaultAsync(x => x.SttId == dto.SttId);

            if (st == null)
                throw new Exception("Stock taking session was not found");

            if (st.Status != TakingStatus.OPEN)
                throw new Exception("Stock taking session has already been completed");

            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("No operator data to submit");

            foreach (var item in dto.Items)
            {
                var action = item.Action?.Trim().ToUpper();

                if (action == "FOUND")
                {
                    if (string.IsNullOrWhiteSpace(item.Epc))
                        throw new Exception("EPC is required for FOUND action");

                    var tag = await _db.Tags
                        .FirstOrDefaultAsync(t => t.EpcTag == item.Epc);

                    if (tag == null)
                        throw new Exception($"Tag with EPC {item.Epc} was not found");

                    var existsInSystem = await _db.StockTakingDetails
                        .AnyAsync(x =>
                            x.SttId == dto.SttId &&
                            x.TagId == tag.Id &&
                            x.Action == TakingAction.SYSTEM
                        );

                    if (!existsInSystem)
                        throw new Exception($"Tag {tag.TagId} is not included in stock taking snapshot");

                    var alreadyFound = await _db.StockTakingDetails
                        .AnyAsync(x =>
                            x.SttId == dto.SttId &&
                            x.TagId == tag.Id &&
                            x.Action == TakingAction.FOUND
                        );

                    if (alreadyFound) continue;

                    _db.StockTakingDetails.Add(new StockTakingDetail
                    {
                        StdId = Guid.NewGuid().ToString(),
                        SttId = dto.SttId,
                        TagId = tag.Id,
                        ItemId = tag.ItemId,
                        Action = TakingAction.FOUND,
                        Remark = item.Remark
                    });
                }
                else if (action == "REMOVE")
                {
                    if (string.IsNullOrWhiteSpace(item.TagId))
                        throw new Exception("TagId is required for REMOVE action");

                    var tag = await _db.Tags
                        .FirstOrDefaultAsync(t => t.TagId == item.TagId);

                    if (tag == null)
                        throw new Exception($"Tag {item.TagId} was not found");

                    var existsInSystem = await _db.StockTakingDetails
                        .AnyAsync(x =>
                            x.SttId == dto.SttId &&
                            x.TagId == tag.Id &&
                            x.Action == TakingAction.SYSTEM
                        );

                    if (!existsInSystem)
                        throw new Exception($"Tag {item.TagId} is not included in this stock taking snapshot");

                    var alreadyRemove = await _db.StockTakingDetails
                        .AnyAsync(x =>
                            x.SttId == dto.SttId &&
                            x.TagId == tag.Id &&
                            x.Action == TakingAction.REMOVE
                        );

                    if (alreadyRemove) continue;

                    _db.StockTakingDetails.Add(new StockTakingDetail
                    {
                        StdId = Guid.NewGuid().ToString(),
                        SttId = dto.SttId,
                        TagId = tag.Id,
                        ItemId = tag.ItemId,
                        Action = TakingAction.REMOVE,
                        Remark = item.Remark
                    });

                    tag.IsDelete = true;
                    tag.UpdatedBy = updatedBy;
                    tag.UpdatedAt = DateTime.UtcNow;
                }

                else if (action == "ADD_MANUAL" || action == "MANUAL_ADD")
                {
                    if (string.IsNullOrWhiteSpace(item.NewTagId))
                        throw new Exception("NewTagId is required for ADD_MANUAL action");

                    if (string.IsNullOrWhiteSpace(item.ItemId))
                        throw new Exception("ItemId is required for ADD_MANUAL action");

                    var tag = await _db.Tags
                        .FirstOrDefaultAsync(x =>
                            x.TagId == item.NewTagId ||
                            x.EpcTag == item.NewTagId
                        );

                    if (tag == null)
                        throw new Exception($"Replacement tag {item.NewTagId} was not found");

                    var alreadyUsed = await _db.StockTakingDetails
                        .AnyAsync(x =>
                            x.SttId == dto.SttId &&
                            x.TagId == tag.Id
                        );

                    if (alreadyUsed) continue;

                    _db.StockTakingDetails.Add(new StockTakingDetail
                    {
                        StdId = Guid.NewGuid().ToString(),
                        SttId = dto.SttId,
                        TagId = tag.Id,
                        ItemId = item.ItemId,
                        Action = TakingAction.ADD_MANUAL,
                        Remark = item.Remark
                    });
                }
                else
                {
                    throw new Exception($"Invalid action: {item.Action}");
                }
            }

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"Operator submit success. SttId={dto.SttId}, Count={dto.Items.Count}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("OperatorSubmitAsync error", ex);
            throw;
        }
    }

}