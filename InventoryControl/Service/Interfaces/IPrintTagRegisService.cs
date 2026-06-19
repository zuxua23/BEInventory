using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IPrintTagRegisService
{
    Task PrintAsync(PrintTagDto dto, string user, string batchNo);
    Task<string> PrintBulkAsync(List<PrintTagDto> list, string user);
    Task RegisterAsync(TagRegistrationDto dto, string user);
    Task RegisterWithItemAsync(TagRegisterWithItemDto dto, string user);
    Task<List<TagInfoDto>> ValidateEpcAsync(TagBulkInfoRequestDto dto);
    Task<List<PrintHistoryResponseDto>> GetAvailableTagsAsync();
    Task<List<TagResponseDto>> GetAllAsync();
    Task<List<StockResponseDto>> GetStockPerItemAsync();
    Task<StockQRDto?> GetByQRAsync(string tagId);
}