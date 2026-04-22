namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
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

        try
        {
            DailyFileLogger.Info($"StockPreparation dimulai. DO: {dto.DoId}, Scanner: {dto.ScannerType}, Code: {dto.Code}");

            var doData = await _db.DOs
                .Include(d => d.Details)
                .FirstOrDefaultAsync(d => d.DoId == dto.DoId);

            if (doData == null)
            {
                DailyFileLogger.Warn($"StockPreparation gagal: DO {dto.DoId} tidak ditemukan");
                throw new Exception("DO tidak ditemukan");
            }

            var location = await _db.Locations
          .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
            {
                DailyFileLogger.Warn($"CreateAsync: Location dengan ID {dto.LocId} tidak ditemukan");
                throw new Exception("Location tidak ditemukan");
            }

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
            {
                DailyFileLogger.Warn($"StockPreparation gagal: Tag {dto.Code} tidak ditemukan");
                throw new Exception("Tag tidak ditemukan");
            }

            if (tag.Status != "IN_STOCK")
            {
                DailyFileLogger.Warn($"StockPreparation gagal: Tag {tag.TagId} status {tag.Status}, harus IN_STOCK");
                throw new Exception($"Tag {tag.TagId} tidak dalam status IN_STOCK");
            }

            var detail = doData.Details
                .FirstOrDefault(d => d.ItemId == tag.ItemId);

            if (detail == null)
            {
                DailyFileLogger.Warn($"StockPreparation gagal: Item {tag.ItemId} tidak ada di DO {dto.DoId}");
                throw new Exception("Item tidak ada dalam DO");
            }

            var reservedCount = await _db.TransactionDetails
                .Where(td =>
                    td.ItemId == tag.ItemId &&
                    td.Transaction.TrsType == "STOCK_PREPARATION" &&
                    td.Transaction.ReferenceId == dto.DoId)
                .CountAsync();

            if (reservedCount >= detail.QtyRequired)
            {
                DailyFileLogger.Warn($"StockPreparation gagal: Qty item {tag.ItemId} sudah terpenuhi di DO {dto.DoId}");
                throw new Exception("Qty item sudah terpenuhi untuk DO ini");
            }

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
            tag.LocationId = location.Id;
            tag.UpdatedBy = user;
            tag.UpdatedAt = DateTime.UtcNow;

            _db.Histories.Add(new HistoryPrint
            {
                Id = Guid.NewGuid().ToString(),
                TagId = tag.Id,
                ItemId = tag.ItemId,
                Type = "STOCK_PREPARATION",
                Reference = dto.DoId,
                Action = "RESERVED_TO_" + location.Name.Replace(" ", "_").ToUpper(),
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });

            if (doData.Status == "DRAFT")
            {
                doData.Status = "PREPARATION";
            }

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"StockPreparation berhasil. DO: {dto.DoId}, Tag: {tag.TagId}, Transaction: {transaction.TrsId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error di StockPreparationService.PrepareAsync", ex);
            throw;
        }
    }
    public async Task PrepareBulkAsync(StockPreparationBulkRequestDto dto, string user)
    {
        using var trx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (dto.ScannedCodes == null || !dto.ScannedCodes.Any())
                throw new Exception("Tidak ada tag yang dikirim");

            DailyFileLogger.Info($"PrepareBulk dimulai. DO: {dto.DoId}, Total: {dto.ScannedCodes.Count}, Scanner: {dto.ScannerType}");

            var doData = await _db.DOs
                .Include(d => d.Details)
                .FirstOrDefaultAsync(d => d.DoId == dto.DoId);

            if (doData == null)
                throw new Exception("DO tidak ditemukan");

            var location = await _db.Locations
                .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
                throw new Exception("Lokasi tidak ditemukan");

            var tags = dto.ScannerType == "RFID"
                ? await _db.Tags.Where(t => dto.ScannedCodes.Contains(t.EpcTag)).ToListAsync()
                : await _db.Tags.Where(t => dto.ScannedCodes.Contains(t.TagId)).ToListAsync();

            if (!tags.Any())
                throw new Exception("Tag tidak ditemukan di database");

            var foundCodes = dto.ScannerType == "RFID"
                ? tags.Select(t => t.EpcTag).ToHashSet()
                : tags.Select(t => t.TagId).ToHashSet();

            var missing = dto.ScannedCodes.Where(c => !foundCodes.Contains(c)).ToList();
            if (missing.Any())
                throw new Exception($"Tag tidak ditemukan: {string.Join(", ", missing)}");

            foreach (var tag in tags)
            {
                if (tag.Status != "IN_STOCK")
                    throw new Exception($"Tag {tag.TagId} status {tag.Status}, harus IN_STOCK");

                var detail = doData.Details.FirstOrDefault(d => d.ItemId == tag.ItemId);
                if (detail == null)
                    throw new Exception($"Item {tag.ItemId} tidak ada di DO ini");
            }

            var reservedPerItem = await _db.TransactionDetails
                .Where(td => td.Transaction.TrsType == "STOCK_PREPARATION"
                          && td.Transaction.ReferenceId == dto.DoId)
                .GroupBy(td => td.ItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ItemId, x => x.Count);

            var newCountPerItem = tags.GroupBy(t => t.ItemId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kv in newCountPerItem)
            {
                var detail = doData.Details.First(d => d.ItemId == kv.Key);
                var alreadyReserved = reservedPerItem.ContainsKey(kv.Key) ? reservedPerItem[kv.Key] : 0;
                var totalAfter = alreadyReserved + kv.Value;

                if (totalAfter > detail.QtyRequired)
                    throw new Exception($"Qty item {kv.Key} kelebihan (scan: {kv.Value}, sudah reserved: {alreadyReserved}, required: {detail.QtyRequired})");
            }

            var transaction = new Transaction
            {
                TrsId = Guid.NewGuid().ToString(),
                TrsType = "STOCK_PREPARATION",
                ReferenceId = dto.DoId,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            _db.Transactions.Add(transaction);

            foreach (var tag in tags)
            {
                _db.TransactionDetails.Add(new Transaction_Detail
                {
                    TrdId = Guid.NewGuid().ToString(),
                    TrsId = transaction.TrsId,
                    TagId = tag.Id,
                    ItemId = tag.ItemId
                });

                tag.Status = "RESERVED";
                tag.LocationId = location.Id;
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.Histories.Add(new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = tag.ItemId,
                    Type = "STOCK_PREPARATION",
                    Reference = dto.DoId,
                    Action = "RESERVED_TO_" + location.Name.Replace(" ", "_").ToUpper(),
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (doData.Status == "DRAFT")
                doData.Status = "PREPARATION";

            await _db.SaveChangesAsync();
            await trx.CommitAsync();

            DailyFileLogger.Info($"PrepareBulk berhasil. DO: {dto.DoId}, Total reserved: {tags.Count}, Trx: {transaction.TrsId}");
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();
            DailyFileLogger.Error("Error PrepareBulkAsync", ex);
            throw;
        }
    }

}
