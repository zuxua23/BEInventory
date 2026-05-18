namespace InventoryControl.Service.Interfaces;

using InventoryControl.DTO;

public interface IItemService
{
    Task<List<ItemResponseDto>> GetAllAsync();
    Task<ItemResponseDto?> GetByIdAsync(string id);
    Task CreateAsync(ItemDto dto, string createdBy);
    Task UpdateAsync(string id, ItemDto dto, string updatedBy);
    Task DeleteAsync(string id, string deletedBy);
}