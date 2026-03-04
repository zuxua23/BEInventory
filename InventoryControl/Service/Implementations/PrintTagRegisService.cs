using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryControl.Service.Implementations;

public class PrintTagRegisService : IPrintTagRegisService
{
    private readonly AppDBContext _db;

    public PrintTagRegisService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<string> PrintAsync(PrintTagDto dto, string user)
    {
        if (dto.Qty <= 0)
            throw new Exception("Qty harus lebih dari 0");

        var batchNo = $"PRN-{DateTime.UtcNow:yyyyMMddHHmmss}";

        for (int i = 0; i < dto.Qty; i++)
        {
            var tag = new Tag
            {
                TagId = Guid.NewGuid().ToString(),
                ItmId = dto.ItemId,
                Status = "PRINTED",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tags.Add(tag);

            _db.Histories.Add(new HistoryPrint
            {
                HisId = Guid.NewGuid().ToString(),
                TagId = tag.TagId,
                ItmId = dto.ItemId,
                Type = "PRINT",
                Reference = batchNo,
                Action = "CREATE",
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return batchNo;
    }


        public async Task RegisterAsync(TagRegistrationDto dto, string user)
        {
            if (!dto.TagIds.Any())
                throw new Exception("Tag tidak boleh kosong");

            var tags = await _db.Tags
                .Where(t => dto.TagIds.Contains(t.TagId))
                .ToListAsync();

            foreach (var tag in tags)
            {
                // Guard
                if (tag.Status != "PRINTED" && tag.Status != "OUT")
                    throw new Exception($"Tag {tag.TagId} tidak bisa diregister");

                tag.Status = "STANDBY";
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.Histories.Add(new HistoryPrint
                {
                    HisId = Guid.NewGuid().ToString(),
                    TagId = tag.TagId,
                    ItmId = tag.ItmId,
                    Type = "TAG_REGISTER",
                    Reference = $"REG-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Action = "STANDBY",
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }

    public async Task<List<PrintHistoryResponseDto>> GetAvailableTagsAsync()
    {
        var result = await _db.Histories
            .Where(h => h.Type == "PRINT")
            .Join(_db.Tags,
                h => h.TagId,
                t => t.TagId,
                (h, t) => new { h, t })
            .Where(x => x.t.Status == "PRINTED" || x.t.Status == "OUT")
            .Join(_db.Items,
                ht => ht.t.ItmId,
                i => i.Id,
                (ht, i) => new PrintHistoryResponseDto
                {
                    TagId = ht.t.TagId,
                    ItemId = ht.t.ItmId,
                    ItemName = i.Name,
                    Status = ht.t.Status,
                    BatchNo = ht.h.Reference,
                    CreatedAt = ht.h.CreatedAt
                })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return result;
    }
}