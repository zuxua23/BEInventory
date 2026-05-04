using InventoryControl.DTO;
using InventoryControl.Entity;

public interface IUserService
{
    Task<List<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto?> GetByIdAsync(string id);

    Task CreateAsync(UserDto dto, string createdBy);
    Task UpdateAsync(string id, UpdateUserDto dto, string updatedBy);
    Task DeleteAsync(string id);
    Task UpdateUserRolesAsync(UpdateUserRoleDto dto, string updatedBy);
    Task UpdatePasswordAsync(string id, UpdatePasswordDto dto, string updatedBy);
}
