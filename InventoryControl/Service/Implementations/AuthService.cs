using Microsoft.EntityFrameworkCore;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Services.Interfaces;
using InventoryControl.Utility;
using StackExchange.Redis;

namespace InventoryControl.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDBContext _db;
        private readonly JwtTokenHelper _jwtHelper;

        public AuthService(AppDBContext db, JwtTokenHelper jwtHelper)
        {
            _db = db;
            _jwtHelper = jwtHelper;
        }


        public async Task<LoginResultDto> ValidateUserAsync(LoginDTO dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username && !u.IsDelete);

            if (user == null)
                throw new Exception("User not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                throw new Exception("Invalid password");

            var roles = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Code)
                .Distinct()
                .ToListAsync();

            var permissions = await (
                from ur in _db.UserRoles
                join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                where ur.UserId == user.Id
                    && !rp.Permission.IsDelete
                    && rp.Permission.IsActive
                select rp.Permission.Code
            ).Distinct().ToListAsync();
            return new LoginResultDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Roles = roles,
                Permissions = permissions
            };
        }
        public async Task<string> GenerateTokenAsync(LoginResultDto user)
        {
            var entityUser = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (entityUser == null)
                throw new Exception("User not found");

            return await _jwtHelper.GenerateTokenAsync(
                entityUser,
                user.Permissions,
                user.Roles
            );
        }
    }
}

