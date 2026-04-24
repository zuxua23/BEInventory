using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface IStockTakingService
{
    Task<string> CreateAsync(StockTakingCreateDto dto, string user);
    Task<List<Tag>> GetStockDataAsync();
    Task ScanAsync(StockTakingScanDto dto);
    Task RemoveAsync(StockTakingRemoveDto dto);
    Task ManualAddAsync(StockTakingManualAddDto dto);
    Task FinalizeAsync(StockTakingFinalizeDto dto, string user);
    Task BulkScanAsync(StockTakingBulkScanDto dto);
    Task<object> GetCompareAsync(string sttId);
}