using InventoryControl.Entity;
using Microsoft.EntityFrameworkCore;

namespace InventoryControl.Database.Seeder;

public class SeedAccess
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = new AppDBContext(
            serviceProvider.GetRequiredService<DbContextOptions<AppDBContext>>());

        await context.Database.MigrateAsync();

        //Permission seeding
        var permissionSeeds = new List<(string Code, string Name, string PerId)>
        {
            ("MASTER_USER_VIEW", "View Master User","PER001"),
            ("MASTER_USER_CREATE", "Create Master User", "PER002"),
            ("MASTER_USER_UPDATE", "Update Master User", "PER003"),
            ("MASTER_USER_DELETE", "Delete Master User", "PER004"),
            ("TRANS_STOCK_IN", "Stock In", "PER005"),
            ("TRANS_STOCK_OUT", "Stock Out", "PER006"),
            ("PRINT_TAG", "Print Tag", "PER007"),
            ("REPRINT_TAG", "Reprint Tag", "PER008")
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

        await context.SaveChangesAsync();

        //Role seeding

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
        //ADMIN → semua permission
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
            .FirstOrDefault(p => p.Code == "TRANS_STOCK_IN");

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
}