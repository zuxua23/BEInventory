using InventoryControl.Entity;

namespace InventoryControl.Service.Interfaces;

public interface IDOService
{
    Task<List<DO>> GetAllAsync();
    Task<DO?> GetByIdAsync(string id);
    Task CreateAsync(DO dto, List<DODetail> details, string createdBy);
    Task DeleteAsync(string id);
}