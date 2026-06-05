using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IPrintTagRegisService
{
    Task PrintAsync(PrintTagDto dto, string user, string batchNo);
    Task<string> PrintBulkAsync(List<PrintTagDto> list, string user);
    Task RegisterAsync(TagRegistrationDto dto, string user);
    Task<List<PrintHistoryResponseDto>> GetAvailableTagsAsync();
    Task<List<TagResponseDto>> GetAllAsync();
    Task<TagStatusCountDto> GetTagStatusCountAsync();
    Task<List<StockResponseDto>> GetStockPerItemAsync();
    Task<List<TagResponseDto>> GetAllTagRegisAsync();
    Task<StockQRDto?> GetByQRAsync(string tagId);
}