using InventoryControl.DTO;
using InventoryControl.Entity;


namespace InventoryControl.Services.Interfaces
{
    public interface ISearchItemService
    {
        Task<List<SearchItemListDto>> GetAllItemsAsync();
        Task<TagDetailDto?> GetTagDetailAsync(string code);
    }
}