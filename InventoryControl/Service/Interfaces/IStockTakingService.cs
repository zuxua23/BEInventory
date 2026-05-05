using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface IStockTakingService
{
    Task<string> CreateAsync(StockTakingCreateDto dto, string user);
    Task<object?> GetActiveAsync();
    Task<List<object>> GetSystemDataAsync(string sttId);
    Task<List<object>> GetSessionTagsAsync(string sttId);
    Task<List<Tag>> GetStockDataAsync();
    Task ScanAsync(StockTakingScanDto dto);
    Task BulkScanAsync(StockTakingBulkScanDto dto);
    Task RemoveAsync(StockTakingRemoveDto dto);
    Task ManualAddAsync(StockTakingManualAddDto dto);
    Task ApplyAdjustmentAsync(string sttId, string user);
    Task FinalizeAsync(StockTakingFinalizeDto dto, string user);
    Task<object> GetCompareAsync(string sttId);
    Task<object> GetProgressAsync(string sttId);
}