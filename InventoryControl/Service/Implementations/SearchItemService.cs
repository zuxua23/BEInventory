using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryControl.Service.Implementations
{
    public class SearchItemService : ISearchItemService
    {
        private readonly AppDBContext _db;

        public SearchItemService(AppDBContext db) => _db = db;

        public async Task<List<SearchItemListDto>> GetAllItemsAsync()
        {
            return await _db.Tags
                .Where(t => t.isDelete == 0)
                .Include(t => t.Item)
                .Select(t => new SearchItemListDto
                {
                    TagId = t.TagId,
                    EpcTag = t.EpcTag,
                    ItemName = t.Item.Name
                })
                .ToListAsync();
        }

        public async Task<TagDetailDto?> GetTagDetailAsync(string code)
        {
            var tag = await _db.Tags
                .Where(t => t.isDelete == 0 &&
                       (t.EpcTag == code || t.TagId == code))
                .Include(t => t.Item)
                .Include(t => t.Location)
                .FirstOrDefaultAsync();

            if (tag == null) return null;

            return new TagDetailDto
            {
                TagId = tag.TagId,
                EpcTag = tag.EpcTag,
                ItemName = tag.Item.Name,
                Location = tag.Location?.Name ?? "-",   // sesuaikan dengan property Location entity kamu
                Status = tag.Status ?? "-"
            };
        }
    }
}