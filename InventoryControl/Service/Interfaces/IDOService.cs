using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface IDOService
{
    Task<List<DOResponseDto>> GetAllAsync();
    Task<DO?> GetByIdAsync(string id);
    Task CreateAsync(DODTO dto, string createdBy);
    Task UpdateAsync(string id, DOUpdateDTO dto);
    Task DeleteAsync(string id);
    Task UpdateStatusAsync(string id, string status);
}