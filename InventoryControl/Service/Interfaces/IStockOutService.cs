using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IStockOutService
{
    Task ScanStockOutAsync(StockOutResponseDto dto, string user);
    Task StockOutAsync(StockOutDto dto, string user);
    Task<List<ItemListDto>> GetItemsAsync(string doId);
    Task<ProgressDto> GetProgressAsync(string doId);
    Task<List<TagDto>> GetTagsAsync(string doId);
}