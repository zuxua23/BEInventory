//using InventoryControl.Entity;
//using Microsoft.EntityFrameworkCore;

//namespace InventoryControl.Database.Seeder;

//public static class SeedAccess
//{
//    public static async Task Initialize(IServiceProvider serviceProvider)
//    {
//        using var context = new AppDBContext(
//            serviceProvider.GetRequiredService<DbContextOptions<AppDBContext>>());

//        await context.Database.MigrateAsync();

//        #region MODULES

//        var moduleSeeds = new List<Module>
//        {
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "ITEM", ModuleName = "Item" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "LOCATION", ModuleName = "Location" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "TAG", ModuleName = "Tag" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "READER", ModuleName = "Reader" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "PICKINGLIST", ModuleName = "Picking List" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "STOCK", ModuleName = "Stock" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "STOCK_TAKING", ModuleName = "Stock Taking" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "USER", ModuleName = "User" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "PERMISSION", ModuleName = "Permission" },
//            new() { Id = Guid.NewGuid().ToString(), ModuleKey = "TRANSACTION", ModuleName = "Transaction" }
//        };

//        foreach (var module in moduleSeeds)
//        {
//            bool exists = await context.Modules
//                .AnyAsync(x => x.ModuleKey == module.ModuleKey);

//            if (!exists)
//            {
//                context.Modules.Add(module);
//            }
//        }

//        await context.SaveChangesAsync();

//        #endregion

//        #region PERMISSIONS

//        var permissionSeeds = new List<(string Module, string Operation, string Name)>
//        {
//            // ITEM
//            ("ITEM", "GET", "View Item"),
//            ("ITEM", "CREATE", "Create Item"),
//            ("ITEM", "UPDATE", "Update Item"),
//            ("ITEM", "DELETE", "Delete Item"),

//            // LOCATION
//            ("LOCATION", "GET", "View Location"),
//            ("LOCATION", "CREATE", "Create Location"),
//            ("LOCATION", "UPDATE", "Update Location"),
//            ("LOCATION", "DELETE", "Delete Location"),

//            // TAG
//            ("TAG", "GET", "View Tag"),
//            ("TAG", "PRINT", "Print Tag"),
//            ("TAG", "REGISTER", "Register Tag"),

//            // READER
//            ("READER", "GET", "View Reader"),
//            ("READER", "CREATE", "Create Reader"),
//            ("READER", "UPDATE", "Update Reader"),
//            ("READER", "DELETE", "Delete Reader"),

//            // PICKINGLIST
//            ("PICKINGLIST", "GET", "View Picking List"),
//            ("PICKINGLIST", "CREATE", "Create Picking List"),
//            ("PICKINGLIST", "UPDATE", "Update Picking List"),
//            ("PICKINGLIST", "DELETE", "Delete Picking List"),

//            // STOCK
//            ("STOCK", "IN", "Stock In"),
//            ("STOCK", "OUT", "Stock Out"),
//            ("STOCK", "PREPARATION", "Stock Preparation"),

//            // STOCK TAKING
//            ("STOCK_TAKING", "GET", "View Stock Taking"),
//            ("STOCK_TAKING", "CREATE", "Create Stock Taking"),
//            ("STOCK_TAKING", "SCAN", "Scan Stock Taking"),
//            ("STOCK_TAKING", "REMOVE", "Remove Stock Taking"),
//            ("STOCK_TAKING", "MANUAL", "Manual Add Stock Taking"),
//            ("STOCK_TAKING", "FINALIZE", "Finalize Stock Taking"),

//            // USER
//            ("USER", "GET", "View User"),
//            ("USER", "CREATE", "Create User"),
//            ("USER", "UPDATE", "Update User"),
//            ("USER", "DELETE", "Delete User"),
//            ("USER", "UPDATE_PASSWORD", "Update Password"),
//            ("USER", "UPDATE_ROLE", "Update User Role"),

//            // PERMISSION
//            ("PERMISSION", "GET", "View Permission"),
//            ("PERMISSION", "CREATE", "Create Permission"),
//            ("PERMISSION", "UPDATE", "Update Permission"),
//            ("PERMISSION", "DELETE", "Delete Permission"),

//            // TRANSACTION
//            ("TRANSACTION", "GET", "View Transaction")
//        };

//        foreach (var item in permissionSeeds)
//        {
//            var module = await context.Modules
//                .FirstAsync(x => x.ModuleKey == item.Module);

//            var code = $"{item.Module}_{item.Operation}";

//            bool exists = await context.Permissions
//                .AnyAsync(x => x.Code == code);

//            if (!exists)
//            {
//                context.Permissions.Add(new Permission
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    ModuleId = module.Id,
//                    Operation = item.Operation,
//                    Code = code,
//                    Name = item.Name,
//                    CreatedBy = "SYSTEM",
//                    CreatedAt = DateTime.UtcNow
//                });
//            }
//        }

//        await context.SaveChangesAsync();

//        #endregion

//        #region ROLES

//        var adminRole = await context.Roles
//            .FirstOrDefaultAsync(x => x.Code == "ADMIN");

//        if (adminRole == null)
//        {
//            adminRole = new Role
//            {
//                Id = Guid.NewGuid().ToString(),
//                Code = "ADMIN",
//                Name = "Administrator"
//            };

//            context.Roles.Add(adminRole);
//        }

//        var operatorRole = await context.Roles
//            .FirstOrDefaultAsync(x => x.Code == "OPERATOR");

//        if (operatorRole == null)
//        {
//            operatorRole = new Role
//            {
//                Id = Guid.NewGuid().ToString(),
//                Code = "OPERATOR",
//                Name = "Operator"
//            };

//            context.Roles.Add(operatorRole);
//        }

//        await context.SaveChangesAsync();

//        #endregion

//        #region DEFAULT LOCATIONS

//        var defaultLocations = new List<Location>
//{
//            new()
//            {
//                Id = Guid.NewGuid().ToString(),
//                LocId = "STAGING",
//                Name = "Shipping Area",
//                Description = "Area barang sebelum keluar dari gudang",
//                IsSystem = true,
//                CreatedBy = "SYSTEM",
//                CreatedAt = DateTime.UtcNow,
//                IsDelete = false
//            }
//        };

//        foreach (var location in defaultLocations)
//        {
//            bool exists = await context.Locations
//                .AnyAsync(x => x.LocId == location.LocId);

//            if (!exists)
//            {
//                context.Locations.Add(location);
//            }
//        }

//        await context.SaveChangesAsync();

//        #endregion

//        #region ROLE PERMISSIONS

//        var allPermissions = await context.Permissions.ToListAsync();

//        // ADMIN => ALL ACCESS
//        foreach (var permission in allPermissions)
//        {
//            bool exists = await context.RolePermissions.AnyAsync(x =>
//                x.RoleId == adminRole.Id &&
//                x.PermissionId == permission.Id);

//            if (!exists)
//            {
//                context.RolePermissions.Add(new Role_Permission
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    RoleId = adminRole.Id,
//                    PermissionId = permission.Id,
//                });
//            }
//        }

//        // OPERATOR => LIMITED ACCESS
//        var operatorPermissions = new List<string>
//        {
//            "STOCK_IN",
//            "STOCK_PREPARATION",
//            "TAG_GET",
//            "PICKINGLIST_GET"
//        };

//        foreach (var code in operatorPermissions)
//        {
//            var permission = allPermissions
//                .FirstOrDefault(x => x.Code == code);

//            if (permission == null)
//                continue;

//            bool exists = await context.RolePermissions.AnyAsync(x =>
//                x.RoleId == operatorRole.Id &&
//                x.PermissionId == permission.Id);

//            if (!exists)
//            {
//                context.RolePermissions.Add(new Role_Permission
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    RoleId = operatorRole.Id,
//                    PermissionId = permission.Id,
//                });
//            }
//        }

//        await context.SaveChangesAsync();

//        #endregion

//        #region USERS

//        var adminUser = await context.Users
//            .FirstOrDefaultAsync(x => x.Username == "admin");

//        if (adminUser == null)
//        {
//            adminUser = new User
//            {
//                Id = Guid.NewGuid().ToString(),
//                UserId = "USR00001",
//                Fullname = "Administrator",
//                Username = "admin",
//                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
//                CreatedBy = "SYSTEM",
//                CreatedAt = DateTime.UtcNow
//            };

//            context.Users.Add(adminUser);

//            await context.SaveChangesAsync();
//        }

//        #endregion

//        #region USER ROLE

//        bool hasAdminRole = await context.UserRoles.AnyAsync(x =>
//            x.UserId == adminUser.Id &&
//            x.RoleId == adminRole.Id);

//        if (!hasAdminRole)
//        {
//            context.UserRoles.Add(new User_Role
//            {
//                Id = Guid.NewGuid().ToString(),
//                UserId = adminUser.Id,
//                RoleId = adminRole.Id,
//                CreatedAt = DateTime.UtcNow
//            });

//            await context.SaveChangesAsync();
//        }

//        #endregion
//    }
//}