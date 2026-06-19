using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface IStockTakingService
{
    Task<List<Location>> GetLocAsync();
    Task<string> CreateAsync(StockTakingCreateDto dto, string user);
    Task<List<Tag>> GetStockDataAsync();
    Task ScanAsync(StockTakingScanDto dto);
    Task RemoveAsync(StockTakingRemoveDto dto);
    Task<ValidateManualTagResultDto> ValidateManualTagAsync(string epcTag, string sttId);
    Task ManualAddAsync(StockTakingManualAddDto dto); 
    Task FinalizeAsync(StockTakingFinalizeDto dto, string user);
    Task BulkScanAsync(StockTakingBulkScanDto dto);
    Task<object> GetCompareAsync(string sttId);
    Task<List<object>> GetSystemDataAsync(string sttId);
    Task<List<StockTakingSessionTagDto>> GetSessionTagsAsync(string sttId);
    Task<object?> GetActiveAsync();
    Task<byte[]> ExportSystemExcelAsync(string sttId);
    Task<string> ExportSystemCsvAsync(string sttId);
    Task<byte[]> ExportCompareExcelAsync(string sttId);
    Task<string> ExportCompareCsvAsync(string sttId);
    Task<object> GetProgressAsync(string sttId);
    Task<List<AvailableTagDto>> GetAvailableTagsAsync(string sttId);
    Task OperatorSubmitAsync(StockTakingOperatorSubmitDto dto);
}