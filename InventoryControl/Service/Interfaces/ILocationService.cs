using InventoryControl.DTO;
using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface ILocationService
{
    Task<List<Location>> GetAllAsync();
    Task<Location?> GetByIdAsync(string id);
    Task CreateAsync(LocationDTO dto, string createdBy);
    Task UpdateAsync(string id, LocationDTO dto, string updatedBy);
    Task DeleteAsync(string id, string deletedBy);
}