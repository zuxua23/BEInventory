using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Services.Interfaces;
using InventoryControl.Utility;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InventoryControl.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDBContext _db;
        private readonly JwtTokenHelper _jwt;
        private readonly IConnectionMultiplexer _redis;

        public AuthService(
            AppDBContext db,
            JwtTokenHelper jwt,
            IConnectionMultiplexer redis)
        {
            _db = db;
            _jwt = jwt;
            _redis = redis;
        }

        // LOGIN API (mengembalikan token)
        public async Task<string> LoginAsync(LoginDTO dto)
        {
            try
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsDelete == 0);

                if (user == null)
                {
                    DailyFileLogger.Warn($"LoginAsync gagal: Username '{dto.Username}' tidak ditemukan.");
                    throw new Exception("User not found");
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                {
                    DailyFileLogger.Warn($"LoginAsync gagal: Password salah untuk Username '{dto.Username}'.");
                    throw new Exception("Invalid password");
                }
                var roles = await _db.UserRoles
                    .Where(ur => ur.UroId == user.UserId)
                    .Select(ur => ur.Role.Code)
                    .Distinct()
                    .ToListAsync();

                var permissions = await _db.UserRoles
                    .Where(ur => ur.UroId == user.UserId)
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Code)
                    .Distinct()
                    .ToListAsync();

                var token = await _jwt.GenerateTokenAsync(user, permissions, roles);
                DailyFileLogger.Info($"LoginAsync berhasil untuk Username '{dto.Username}', UserId: {user.UserId}.");

                return token;
            }
            catch (Exception ex)
            {
                DailyFileLogger.Error($"Error di LoginAsync untuk Username '{dto.Username}'.", ex);
                throw;
            }
        }

        // LOGIN WEB (mengembalikan User object)
        public async Task<User> LoginWebAsync(LoginDTO dto)
        {
            try
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(x => x.Username == dto.Username && x.IsDelete == 0);

                if (user == null)
                {
                    DailyFileLogger.Warn($"LoginWebAsync gagal: Username '{dto.Username}' tidak ditemukan.");
                    throw new Exception("Username tidak ditemukan");
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                {
                    DailyFileLogger.Warn($"LoginWebAsync gagal: Password salah untuk Username '{dto.Username}'.");
                    throw new Exception("Password salah");
                }

                DailyFileLogger.Info($"LoginWebAsync berhasil untuk Username '{dto.Username}', UserId: {user.UserId}.");
                return user;
            }
            catch (Exception ex)
            {
                DailyFileLogger.Error($"Error di LoginWebAsync untuk Username '{dto.Username}'.", ex);
                throw;
            }
        }

        // LOGOUT
        public async Task LogoutAsync(string userId)
        {
            try
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.IsDelete == 0);

                if (user == null)
                {
                    DailyFileLogger.Warn($"LogoutAsync gagal: UserId {userId} tidak ditemukan.");
                    throw new Exception("User not found");
                }

                var redisDb = _redis.GetDatabase();
                await redisDb.KeyDeleteAsync($"jwt:{userId}");

                DailyFileLogger.Info($"LogoutAsync berhasil untuk UserId {userId}.");
            }
            catch (Exception ex)
            {
                DailyFileLogger.Error($"Error di LogoutAsync untuk UserId {userId}.", ex);
                throw;
            }
        }
    }
}
