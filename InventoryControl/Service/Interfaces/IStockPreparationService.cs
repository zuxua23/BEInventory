using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IStockPreparationService
{
    Task PrepareAsync(StockPreparationRequestDto dto, string user);
    Task PrepareBulkAsync(StockPreparationBulkRequestDto dto, string user);

    //Task PrepareAsync(StockPreparationDto dto, string user);
}