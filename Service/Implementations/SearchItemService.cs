
using InventoryControl.Service.Interfaces;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Models;
using InventoryControl.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryControl.Service.Implementations;

public class SearchItemService : ISearchItemService
{
    private readonly AppDBContext _db;

    public SearchItemService(AppDBContext db) => _db = db;

    public async Task<List<SearchItemListDto>> GetAllItemsAsync()
    {
        return await _db.Tags
            .Where(t => (t.IsDelete == false || t.IsDelete == null) &&
                (t.Status == TagStatus.RESERVED || t.Status == TagStatus.IN_STOCK))
            .Include(t => t.Item)
            .Include(t => t.Location)
            .Select(t => new SearchItemListDto
            {
                TagId = t.TagId,
                EpcTag = t.EpcTag,
                ItemName = t.Item.Name,
                Location = t.Location != null ? t.Location.Name : "-"
            })
            .ToListAsync();
    }

    public async Task<TagDetailDto?> GetTagDetailAsync(string code)
    {
        var tag = await _db.Tags
            .Where(t => (t.IsDelete == false || t.IsDelete == null) &&
                (t.EpcTag == code || t.TagId == code))
            .Include(t => t.Item)
            .Include(t => t.Location)
            .FirstOrDefaultAsync();

        if (tag == null) return null;

        var statusDisplay = tag.Status switch
        {
            TagStatus.RESERVED => "PREPARATION",
            TagStatus.IN_STOCK => "STOCK IN",
            _ => tag.Status.ToString()
        };

        return new TagDetailDto
        {
            TagId = tag.TagId,
            EpcTag = tag.EpcTag,
            ItemName = tag.Item?.Name ?? "-",
            Location = tag.Location?.Name ?? "-",
            Status = statusDisplay
        };
    }
}