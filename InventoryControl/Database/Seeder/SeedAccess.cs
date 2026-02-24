using InventoryControl.Entity;
using Microsoft.EntityFrameworkCore;
using System;

namespace InventoryControl.Database.Seeder;

public class SeedAccess
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = new AppDBContext(
            serviceProvider.GetRequiredService<DbContextOptions<AppDBContext>>());

        if (!context.Permissions.Any())
        {
            var permissions = new List<Permission>
            {
                new Permission {Id = Guid.NewGuid().ToString(), Code = "MASTER_ITEM_VIEW", Name = "View Master Item", CreatedBy = "SYSTEM", PerId = "PER001"},
                new Permission {Id = Guid.NewGuid().ToString(), Code = "MASTER_ITEM_CREATE", Name = "Create Master Item", CreatedBy = "SYSTEM",PerId = "PER002"  },
                new Permission {Id = Guid.NewGuid().ToString(), Code = "MASTER_ITEM_UPDATE", Name = "Update Master Item", CreatedBy = "SYSTEM", PerId = "PER003"},
                new Permission {Id = Guid.NewGuid().ToString(), Code = "MASTER_ITEM_DELETE", Name = "Delete Master Item", CreatedBy = "SYSTEM", PerId = "PER004"},

                new Permission {Id = Guid.NewGuid().ToString(), Code = "TRANS_STOCK_IN", Name = "Stock In", CreatedBy = "SYSTEM", PerId = "PER005"},
                new Permission {Id = Guid.NewGuid().ToString(), Code = "TRANS_STOCK_OUT", Name = "Stock Out", CreatedBy = "SYSTEM", PerId = "PER006"},

                new Permission {Id = Guid.NewGuid().ToString(), Code = "PRINT_TAG", Name = "Print Tag", CreatedBy = "SYSTEM", PerId = "PER007"},
                new Permission {Id = Guid.NewGuid().ToString(), Code = "REPRINT_TAG", Name = "Reprint Tag", CreatedBy = "SYSTEM"       , PerId = "PER008"}
            };

            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();
        }

        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "admin" && u.IsDelete == 0);

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
                CreatedAt = DateTime.UtcNow,
                IsDelete = 0
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        if (!context.Roles.Any())
        {
            var operatorRole = new Role
            {
                Id = Guid.NewGuid().ToString(),
                RolId="ROL001",
                Code = "OPERATOR",
                Name = "Operator"
            };

            var adminRole = new Role
            {
                Id = Guid.NewGuid().ToString(),
                RolId="ROL002",
                Code = "ADMIN",
                Name = "Administrator"
            };

            context.Roles.AddRange(operatorRole, adminRole);
            await context.SaveChangesAsync();

            // Assign permission ke role
            var stockInPermission = context.Permissions
                .First(p => p.Code == "TRANS_STOCK_IN");

            context.RolePermissions.Add(new Role_Permission
            {
                Id = Guid.NewGuid().ToString(),
                Code = "RPR001",
                RolId = operatorRole.RolId,
                PerId = stockInPermission.PerId,
                CreatedBy = "SYSTEM",
                CreatedAt = DateTime.UtcNow
            });

            var allPermissions = context.Permissions.ToList();
            var index = 2;
            foreach (var permission in allPermissions)
            {
                context.RolePermissions.Add(new Role_Permission
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = $"RPR{index:000}",
                    RolId = adminRole.RolId,
                    PerId = permission.PerId,
                    CreatedBy = "SYSTEM",
                    CreatedAt = DateTime.UtcNow
                });
                index++;
            }

            await context.SaveChangesAsync();
        }

        var hasAdminRole = await context.UserRoles.AnyAsync(ur =>
            ur.UroId == adminUser.UserId && ur.RolId == "ROL002");

        if (!hasAdminRole)
        {
            context.UserRoles.Add(new User_Role
            {
                Id = Guid.NewGuid().ToString(),
                UroId = adminUser.UserId,
                RolId = "ROL002",
                CreatedBy = "SYSTEM",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}
