using Microsoft.EntityFrameworkCore;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Utility; 

public class UserService : IUserService
{
    private readonly AppDBContext _db;
    
    public UserService(AppDBContext db)
    {
        _db = db;
    }

    // READ ALL
    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        try
        {
            var result = await _db.Users
                .Where(x => x.IsDelete)
                .Select(x => new UserResponseDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Fullname = x.Fullname,
                    Username = x.Username
                })
                .ToListAsync();

            DailyFileLogger.Info($"GetAllAsync berhasil, total {result.Count} user.");
            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di GetAllAsync.", ex);
            throw; // lempar exception agar tetap bisa ditangani caller
        }
    }

    // READ BY ID
    public async Task<UserResponseDto?> GetByIdAsync(string id)
    {
        try
        {
            var user = await _db.Users
                .Where(x => x.Id == id && x.IsDelete)
                .Select(x => new UserResponseDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Fullname = x.Fullname,
                    Username = x.Username
                })
                .FirstOrDefaultAsync();

            if (user != null)
                DailyFileLogger.Info($"GetByIdAsync berhasil untuk ID {id}.");
            else
                DailyFileLogger.Warn($"GetByIdAsync: User dengan ID {id} tidak ditemukan.");

            return user;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di GetByIdAsync untuk ID {id}.", ex);
            throw;
        }
    }

    // CREATE
    public async Task CreateAsync(UserDto dto, string createdBy)
    {
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserId = dto.UserId,
                Fullname = dto.Fullname,
                Username = dto.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"CreateAsync berhasil untuk UserID {dto.UserId}.");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di CreateAsync untuk UserID {dto.UserId}.", ex);
            throw;
        }
    }

    // UPDATE
    public async Task UpdateAsync(string id, UpdateUserDto dto, string updatedBy)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null || user.IsDelete == true)
            {
                DailyFileLogger.Warn($"UpdateAsync: User dengan ID {id} tidak ditemukan.");
                throw new Exception("User tidak ditemukan");
            }

            user.Fullname = dto.Fullname;
            user.Username = dto.Username;
            user.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"UpdateAsync berhasil untuk ID {id}.");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di UpdateAsync untuk ID {id}.", ex);
            throw;
        }
    }

    // DELETE (SOFT)
    public async Task DeleteAsync(string id)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                DailyFileLogger.Warn($"DeleteAsync: User dengan ID {id} tidak ditemukan.");
                throw new Exception("User tidak ditemukan");
            }

            user.IsDelete = true;
            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"DeleteAsync berhasil untuk ID {id} (soft delete).");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di DeleteAsync untuk ID {id}.", ex);
            throw;
        }
    }
}
