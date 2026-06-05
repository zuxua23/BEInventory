using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface IPickingListService
{
    Task<List<DOResponseDto>> GetAllAsync();
    Task<DOStatusCountDto> GetDOStatusCountAsync();
    Task<int> GetAvailableStockForEditAsync(string itemId, string? doId);
    Task<DO?> GetByIdAsync(string id);
    Task CreateAsync(PickingListDTO dto, string createdBy);
    Task UpdateAsync(string id, PickingListDTO dto, string updatedBy);
    Task DeleteAsync(string id, string deletedBy);
}