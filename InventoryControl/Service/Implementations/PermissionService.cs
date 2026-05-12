namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class PermissionService : IPermissionService
{
    private readonly AppDBContext _db;

    public PermissionService(AppDBContext db)
    {
        _db = db;
    }

    public async Task Create(
        RoleRequestDto dto,
        string user
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Creating new role with code '{dto.RoleCode}'.",
                user
            );

            var roleExists = await _db.Roles
                .AnyAsync(x => x.Code == dto.RoleCode);

            if (roleExists)
            {
                DailyFileLogger.Warn(
                    $"Role code '{dto.RoleCode}' already exists.",
                    user
                );

                throw new Exception(
                    "Role code already exists."
                );
            }

            var role = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Code = dto.RoleCode,
                Name = dto.RoleName
            };

            _db.Roles.Add(role);

            await SavePermissions(
                role.Id,
                dto.Permissions,
                user
            );

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Role successfully created with code '{dto.RoleCode}'.",
                user
            );

            DailyFileLogger.Audit(
                action: "CREATE",
                entity: "ROLE",
                entityId: role.Code,
                performedBy: user,
                description:
                    $"Created role '{dto.RoleName}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while creating role '{dto.RoleCode}'.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task Update(
        string id,
        RoleRequestDto dto,
        string user
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating role with ID '{id}'.",
                user
            );

            var role = await _db.Roles.FindAsync(id);

            if (role == null)
            {
                DailyFileLogger.Warn(
                    $"Update failed. Role with ID '{id}' was not found.",
                    user
                );

                throw new Exception(
                    "Role not found."
                );
            }

            var oldRoleCode = role.Code;
            var oldRoleName = role.Name;

            role.Name = dto.RoleName;
            role.Code = dto.RoleCode;

            var existingPermissions = _db.RolePermissions
                .Where(x => x.RoleId == id);

            _db.RolePermissions.RemoveRange(
                existingPermissions
            );

            await SavePermissions(
                id,
                dto.Permissions,
                user
            );

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Role successfully updated. ID='{id}'.",
                user
            );

            DailyFileLogger.Audit(
                action: "UPDATE",
                entity: "ROLE",
                entityId: role.Code,
                performedBy: user,
                description:
                    $"Updated role from " +
                    $"Code='{oldRoleCode}', Name='{oldRoleName}' " +
                    $"to Code='{dto.RoleCode}', Name='{dto.RoleName}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while updating role with ID '{id}'.",
                ex,
                user
            );

            throw;
        }
    }

    private async Task SavePermissions(
        string roleId,
        Dictionary<string, List<string>> permissions,
        string user
    )
    {
        try
        {
            foreach (var module in permissions)
            {
                var moduleKey = module.Key;

                foreach (var action in module.Value)
                {
                    var permission = await _db.Permissions
                        .Include(p => p.Module)
                        .FirstOrDefaultAsync(x =>
                            x.Module.ModuleKey == moduleKey &&
                            x.Operation == action
                        );

                    if (permission == null)
                    {
                        DailyFileLogger.Warn(
                            $"Permission not found for Module='{moduleKey}', Action='{action}'.",
                            user
                        );

                        continue;
                    }

                    _db.RolePermissions.Add(
                        new Role_Permission
                        {
                            Id = Guid.NewGuid().ToString(),
                            RoleId = roleId,
                            PermissionId = permission.Id
                        }
                    );
                }
            }

            DailyFileLogger.Info(
                $"Permissions successfully assigned to RoleId '{roleId}'.",
                user
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while assigning permissions to RoleId '{roleId}'.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task<RoleResponseDto> GetById(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving role detail for ID '{id}'."
            );

            var role = await _db.Roles.FindAsync(id);

            if (role == null)
            {
                DailyFileLogger.Warn(
                    $"Role with ID '{id}' was not found."
                );

                throw new Exception(
                    "Role not found."
                );
            }

            var rolePermissions = await _db.RolePermissions
                .Where(x => x.RoleId == id)
                .Include(x => x.Permission)
                .ThenInclude(p => p.Module)
                .ToListAsync();

            var permissions = rolePermissions
                .GroupBy(x => x.Permission.Module.ModuleKey)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Permission.Operation).ToList()
                );

            DailyFileLogger.Info(
                $"Successfully retrieved role with ID '{id}'."
            );

            return new RoleResponseDto
            {
                Id = role.Id,
                RoleCode = role.Code,
                RoleName = role.Name,
                Permissions = permissions
            };
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving role with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task<object> GetModules()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving active modules and permissions."
            );

            var result = await _db.Modules
                .Where(m => m.IsActive)
                .Select(m => new
                {
                    moduleKey = m.ModuleKey,
                    moduleName = m.ModuleName,
                    permissions = m.Permissions
                        .Select(p => p.Operation)
                        .Distinct()
                        .ToList()
                })
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} active module(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving modules.",
                ex
            );

            throw;
        }
    }

    public async Task<List<Role>> GetAll()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving all roles."
            );

            var result = await _db.Roles
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} role(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving all roles.",
                ex
            );

            throw;
        }
    }

    public async Task Delete(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Deleting role with ID '{id}'."
            );

            var role = await _db.Roles.FindAsync(id);

            if (role == null)
            {
                DailyFileLogger.Warn(
                    $"Delete failed. Role with ID '{id}' was not found."
                );

                return;
            }

            var permissions = _db.RolePermissions
                .Where(x => x.RoleId == id);

            _db.RolePermissions.RemoveRange(
                permissions
            );

            _db.Roles.Remove(role);

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Role successfully deleted. ID='{id}'."
            );

            DailyFileLogger.Audit(
                action: "DELETE",
                entity: "ROLE",
                entityId: role.Code,
                performedBy: "SYSTEM",
                description:
                    $"Deleted role '{role.Name}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while deleting role with ID '{id}'.",
                ex
            );

            throw;
        }
    }
}