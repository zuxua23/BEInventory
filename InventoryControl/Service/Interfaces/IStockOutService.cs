using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IStockOutService
{
    Task ScanStockOutAsync(string doId, string readerId, string epc, string user);
    Task StockOutAsync(StockOutDto dto, string user);
}