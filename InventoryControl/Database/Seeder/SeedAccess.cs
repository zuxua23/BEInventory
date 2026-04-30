using InventoryControl.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryControl.Database.Seeder;

public class SeedAccess
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = new AppDBContext(
            serviceProvider.GetRequiredService<DbContextOptions<AppDBContext>>());

        await context.Database.MigrateAsync();

        var permissionSeeds = new List<(string Code, string Name, string PerId)>
{
    // MASTER ITEM
    ("ITEM_GET", "View Master Item", "PER001"),
    ("ITEM_CREATE", "Create Master Item", "PER002"),
    ("ITEM_UPDATE", "Update Master Item", "PER003"),
    ("ITEM_DELETE", "Delete Master Item", "PER004"),

    // TAG
    ("TAG_PRINT", "Print Tag", "PER005"),
    ("TAG_REGISTER", "Reprint Tag", "PER006"),

    // STOCK
    ("STOCK_IN", "Stock In", "PER007"),
    ("STOCK_PREPARATION", "Stock Preparation", "PER008"),
    ("STOCK_OUT", "Stock Out", "PER009"),

    // PICKINGLIST
    ("PICKINGLIST_GET", "View PICKINGLIST", "PER010"),
    ("PICKINGLIST_CREATE", "Create PICKINGLIST", "PER011"),
    ("PICKINGLIST_UPDATE", "Update PICKINGLIST", "PER012"),
    ("PICKINGLIST_UPDATE_STATUS", "Update PICKINGLIST", "PER030"),
    ("PICKINGLIST_DELETE", "Delete PICKINGLIST", "PER031"),

    // LOCATION
    ("LOCATION_GET", "View Location", "PER013"),
    ("LOCATION_CREATE", "Create Location", "PER014"),
    ("LOCATION_UPDATE", "Update Location", "PER015"),
    ("LOCATION_DELETE", "Delete Location", "PER016"),

    // READER
    ("READER_GET", "View Reader", "PER017"),
    ("READER_CREATE", "Create Reader", "PER018"),
    ("READER_DELETE", "Delete Reader", "PER019"),
    ("READER_UPDATE", "Update Reader", "PER020"),

    // STOCK TAKING
    ("STOCK_TAKING_CREATE", "Create Stock Taking", "PER021"),
    ("STOCK_TAKING_SCAN", "Scan Stock Taking", "PER022"),
    ("STOCK_TAKING_REMOVE", "Remove Stock Taking", "PER023"),
    ("STOCK_TAKING_MANUAL", "Manual Stock Taking", "PER024"),
    ("STOCK_TAKING_FINALIZE", "Finalize Stock Taking", "PER025"),

            ("USER_GET", "View Master User","PER026"),
            ("USER_CREATE", "Create Master User", "PER027"),
            ("USER_UPDATE", "Update Master User", "PER028"),
            ("USER_DELETE", "Delete Master User", "PER029"),

            ("PERMISSION_GET", "View Master PERMISSION","PER034"),
            ("PERMISSION_CREATE", "Create Master PERMISSION", "PER035"),
            ("PERMISSION_UPDATE", "Update Master PERMISSION", "PER032"),
            ("PERMISSION_DELETE", "Delete Master PERMISSION", "PER033"),
            ("TRANSACTION_GET", "VIEW Master PERMISSION", "PER037"),
            ("TAG_GET", "VIEW Master TAG", "PER036"),
            ("PERMISSION_GET", "List Search", "PER038"),
            ("PERMISSION_DETAIL", "Detail Search", "PER039"),
};
        foreach (var (code, name, perid) in permissionSeeds)
        {
            bool exists = await context.Permissions
                .AnyAsync(p => p.Code == code);

            if (!exists)
            {
                context.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = code,
                    Name = name,
                    PerId = perid,
                    CreatedBy = "SYSTEM",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        var permissions = await context.Permissions.ToListAsync();

        foreach (var perm in permissions)
        {
            var parts = perm.Code.Split('_');

            if (parts.Length < 2) continue;

            var moduleKey = string.Join("_", parts.Take(parts.Length - 1));

            perm.Operation = parts.Last();
            var moduleName = moduleKey;

            var module = await context.Modules
                .FirstOrDefaultAsync(m => m.ModuleKey == moduleKey);

            if (module == null)
            {
                module = new Module
                {
                    Id = Guid.NewGuid().ToString(),
                    ModuleKey = moduleKey,
                    ModuleName = moduleName
                };

                context.Modules.Add(module);
                await context.SaveChangesAsync();
            }

            perm.ModuleId = module.Id;
        }

        //await _context.SaveChangesAsync();

        await context.SaveChangesAsync();


        var adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Code == "ADMIN");

        if (adminRole == null)
        {
            adminRole = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Code = "ADMIN",
                Name = "Administrator"
            };

            context.Roles.Add(adminRole);
        }

        var operatorRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Code == "OPERATOR");

        if (operatorRole == null)
        {
            operatorRole = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Code = "OPERATOR",
                Name = "Operator"
            };

            context.Roles.Add(operatorRole);
        }

        await context.SaveChangesAsync();


        var allPermissions = await context.Permissions.ToListAsync();
        var index = 2;
        foreach (var permission in allPermissions)
        {
            bool exists = await context.RolePermissions.AnyAsync(rp =>
                rp.RoleId == adminRole.Id &&
                rp.PermissionId == permission.Id);

            if (!exists)
            {
                context.RolePermissions.Add(new Role_Permission
                {
                    Id = Guid.NewGuid().ToString(),
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var stockInPermission = allPermissions
            .FirstOrDefault(p => p.Code == "STOCK_IN");

        if (stockInPermission != null)
        {
            bool exists = await context.RolePermissions.AnyAsync(rp =>
                rp.RoleId == operatorRole.Id &&
                rp.PermissionId == stockInPermission.Id);

            if (!exists)
            {
                context.RolePermissions.Add(new Role_Permission
                {
                    Id = Guid.NewGuid().ToString(),
                    RoleId = operatorRole.Id,
                    PermissionId = stockInPermission.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();


        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "admin");

        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "USR001",
                Fullname = "Administrator",
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                CreatedBy = "SYSTEM",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }


        bool hasAdminRole = await context.UserRoles.AnyAsync(ur =>
            ur.UserId == adminUser.Id &&
            ur.RoleId == adminRole.Id);

        if (!hasAdminRole)
        {
            context.UserRoles.Add(new User_Role
            {
                Id = Guid.NewGuid().ToString(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
    public async Task SeedModuleFromPermission(IServiceProvider serviceProvider)
    {
        using var _context = new AppDBContext(
    serviceProvider.GetRequiredService<DbContextOptions<AppDBContext>>());

        await _context.Database.MigrateAsync();

        var permissions = await _context.Permissions.ToListAsync();

        foreach (var perm in permissions)
        {
            var parts = perm.Code.Split('_');

            if (parts.Length < 2) continue;

            var moduleKey = string.Join("_", parts.Take(parts.Length - 1));

            var operation = parts.Last();
            var moduleName = moduleKey; 

            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.ModuleKey == moduleKey);

            if (module == null)
            {
                module = new Module
                {
                    Id = Guid.NewGuid().ToString(),
                    ModuleKey = moduleKey,
                    ModuleName = moduleName
                };

                _context.Modules.Add(module);
                await _context.SaveChangesAsync();
            }

            perm.ModuleId = module.Id;
        }

        await _context.SaveChangesAsync();
    }
}