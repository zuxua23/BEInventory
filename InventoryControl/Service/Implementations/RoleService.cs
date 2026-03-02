namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

public class RoleService : IRoleService
{
    private readonly AppDBContext _db;

    public RoleService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<List<RoleResponseDto>> GetAllAsync()
    {
        return await _db.Roles
            .Where(x => x.IsDelete == false)
            .Select(x => new RoleResponseDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name
            })
            .ToListAsync();
    }

    public async Task CreateAsync(RoleDto dto, string createdBy)
    {
        var exists = await _db.Roles
            .AnyAsync(x => x.Code == dto.Code && x.IsDelete == false);

        if (exists)
            throw new Exception("Role sudah ada.");

        var role = new Role
        {
            Id = Guid.NewGuid().ToString(),
            Code = dto.Code,
            Name = dto.Name,
            //CreatedAt = DateTime.UtcNow,
            //CreatedBy = createdBy,
            IsDelete = false
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
    }

    public async Task AssignPermissionsAsync(string roleId, List<string> permissionIds)
    {
        var role = await _db.Roles.FindAsync(roleId);
        if (role == null || role.IsDelete)
            throw new Exception("Role tidak ditemukan.");

        foreach (var permissionId in permissionIds)
        {
            bool exists = await _db.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (!exists)
            {
                _db.RolePermissions.Add(new Role_Permission
                {
                    Id = Guid.NewGuid().ToString(),
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task AssignRolesToUserAsync(string userId, List<string> roleIds)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null || user.IsDelete)
            throw new Exception("User tidak ditemukan.");

        foreach (var roleId in roleIds)
        {
            bool exists = await _db.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (!exists)
            {
                _db.UserRoles.Add(new User_Role
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}