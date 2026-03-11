namespace InventoryControl.Service.Interfaces;

using InventoryControl.DTO;

public interface IPermissionService
{
    Task<List<PermissionResponseDto>> GetAllAsync();
    Task<PermissionResponseDto?> GetByIdAsync(string id);
    Task CreateAsync(PermissionDto dto, string createdBy);
    Task UpdateAsync(string id, PermissionUpdateDto dto, string updatedBy);
    Task DeleteAsync(string id);
}