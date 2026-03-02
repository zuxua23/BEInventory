
using InventoryControl.Entity;
using InventoryControl.Utility;

namespace InventoryControl.Database.Seeder;

public static class UserSeeder
{
    public static async Task Seed(AppDBContext db)
    {
        var passwordHash = new PasswordHash();
        var idUser = UuidGen.GenerateUuid();
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "USR001",
                Fullname = "Administrator",
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                CreatedAt = DateTime.Now,
                CreatedBy = "SYSTEM",
            });

            await db.SaveChangesAsync();
        }
    }
}