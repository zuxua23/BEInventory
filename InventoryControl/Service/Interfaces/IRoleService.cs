using InventoryControl.DTO;

namespace InventoryControl.Service.Interfaces;

public interface IRoleService
{
    Task<List<RoleResponseDto>> GetAllAsync();
    Task CreateAsync(RoleDto dto, string createdBy);
    Task AssignPermissionsAsync(string roleId, List<string> permissionIds);
    Task AssignRolesToUserAsync(string userId, List<string> roleIds);
}