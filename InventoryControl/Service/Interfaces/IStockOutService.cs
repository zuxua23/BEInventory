using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IStockOutService
{
    Task ScanStockOutAsync(StockOutResponseDto dto, string user);
    Task StockOutAsync(StockOutDto dto, string user);
}