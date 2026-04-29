using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IStockInService
{
    //Task StockInAsync(StockInRequestDto dto, string user);
    Task StockInAsync(StockInDto dto, string user);
    Task<TagResponseDto?> GetTagByCodeAsync(string code, string scannerType);

}