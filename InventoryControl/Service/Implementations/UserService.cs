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

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        try
        {
            var result = await _db.Users
                .Where(x => x.IsDelete == false)
                .Select(x => new UserResponseDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Fullname = x.Fullname,
                    Username = x.Username,
                    Roles = _db.UserRoles
                        .Where(ur => ur.UserId == x.Id)
                        .Include(ur => ur.Role)
                        .Select(ur => ur.Role.Name)
                        .ToList()
                })
                .ToListAsync();

            DailyFileLogger.Info($"GetAllAsync berhasil, total {result.Count} user.");
            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di GetAllAsync.", ex);
            throw; 
        }
    }

    public async Task<UserResponseDto?> GetByIdAsync(string id)
    {
        try
        {
            var user = await _db.Users
                .Where(x => x.Id == id && x.IsDelete == false)
                .Select(x => new UserResponseDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Fullname = x.Fullname,
                    Username = x.Username,
                    Roles = _db.UserRoles
                        .Where(ur => ur.UserId == x.Id)
                        .Include(ur => ur.Role)
                        .Select(ur => ur.Role.Name)
                        .ToList()
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

    public async Task CreateAsync(UserDto dto, string createdBy)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var lastItem = await _db.Users
                .IgnoreQueryFilters()
                .OrderByDescending(x => x.UserId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastItem != null)
            {
                var lastNumber = int.Parse(lastItem.UserId.Replace("USR", ""));
                nextNumber = lastNumber + 1;
            }

            string newId = "USR" + nextNumber.ToString("D5");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new Exception("Password cannot be empty");

            if (string.IsNullOrWhiteSpace(dto.Username))
                throw new Exception("Username wajib diisi");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserId = newId,
                Fullname = dto.Fullname,
                Username = dto.Username,
                Password = hashedPassword,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                var validRoles = await _db.Roles
                    .Where(r => dto.RoleIds.Contains(r.Id) && !r.IsDelete)
                    .Select(r => r.Id)
                    .ToListAsync();

                var userRoles = validRoles.Select(roleId => new User_Role
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    RoleId = roleId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.UserRoles.AddRangeAsync(userRoles);
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            DailyFileLogger.Info($"CreateAsync berhasil untuk UserID {newId}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(); 

            DailyFileLogger.Error("Error di CreateAsync", ex);
            throw;
        }
    }

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
    public async Task UpdateUserRolesAsync(UpdateUserRoleDto dto, string updatedBy)
    {
        using var trx = await _db.Database.BeginTransactionAsync();

        try
        {
            var userExists = await _db.Users
                .AnyAsync(x => x.Id == dto.UserId && !x.IsDelete);

            if (!userExists)
                throw new Exception("User tidak ditemukan");

            var oldRoles = _db.UserRoles.Where(x => x.UserId == dto.UserId);
            _db.UserRoles.RemoveRange(oldRoles);

            if (dto.Roles != null && dto.Roles.Any())
            {
                var validRoles = await _db.Roles
                    .Where(r => dto.Roles.Contains(r.Id) && !r.IsDelete)
                    .Select(r => r.Id)
                    .ToListAsync();

                var userRoles = validRoles.Select(roleId => new User_Role
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = dto.UserId,
                    RoleId = roleId,
                    CreatedBy = updatedBy,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.UserRoles.AddRangeAsync(userRoles);
            }

            await _db.SaveChangesAsync();
            await trx.CommitAsync();
        }
        catch (Exception)
        {
            await trx.RollbackAsync();
            throw;
        }
    }

    public async Task UpdatePasswordAsync(string id, UpdatePasswordDto dto, string updatedBy)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);

            if (user == null || user.IsDelete)
                throw new Exception("User tidak ditemukan");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new Exception("Password tidak boleh kosong");

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.UpdatedBy = updatedBy;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"UpdatePasswordAsync berhasil untuk ID {id}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di UpdatePasswordAsync untuk ID {id}", ex);
            throw;
        }
    }

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
