using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface ILocationService
{
    Task<List<Location>> GetAllAsync();
    Task<Location?> GetByIdAsync(string id);
    Task CreateAsync(Location dto, string createdBy);
    Task UpdateAsync(string id, Location dto, string updatedBy);
    Task DeleteAsync(string id);
}