using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface ISearchItemService
{
    Task<List<SearchItemListDto>> GetAllItemsAsync();
    Task<TagDetailDto?> GetTagDetailAsync(string code);
}
