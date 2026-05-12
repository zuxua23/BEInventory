namespace InventoryControl.Services.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Services.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class AuthService : IAuthService
{
    private readonly AppDBContext _db;
    private readonly JwtTokenHelper _jwtHelper;

    public AuthService(
        AppDBContext db,
        JwtTokenHelper jwtHelper
    )
    {
        _db = db;
        _jwtHelper = jwtHelper;
    }

    public async Task<LoginResultDto>
        ValidateUserAsync(LoginDTO dto)
    {
        try
        {
            DailyFileLogger.Info(
                $"Login attempt detected for username '{dto.Username}'."
            );

            if (
                string.IsNullOrWhiteSpace(
                    dto.Username
                )
            )
            {
                DailyFileLogger.Warn(
                    "Login failed because username is empty."
                );

                throw new Exception(
                    "Username cannot be empty."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Password
                )
            )
            {
                DailyFileLogger.Warn(
                    $"Login failed for username '{dto.Username}' because password is empty."
                );

                throw new Exception(
                    "Password cannot be empty."
                );
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.Username &&
                    !u.IsDelete
                );

            if (user == null)
            {
                DailyFileLogger.Warn(
                    $"Login failed. Username '{dto.Username}' was not found."
                );

                throw new Exception(
                    "User not found."
                );
            }

            bool validPassword =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.Password
                );

            if (!validPassword)
            {
                DailyFileLogger.Warn(
                    $"Invalid password attempt for username '{dto.Username}'."
                );

                throw new Exception(
                    "Invalid password."
                );
            }

            var roles = await _db.UserRoles
                .Where(ur =>
                    ur.UserId == user.Id
                )
                .Select(ur =>
                    ur.Role.Code
                )
                .Distinct()
                .ToListAsync();

            var permissions = await (
                from ur in _db.UserRoles

                join rp in _db.RolePermissions
                    on ur.RoleId equals rp.RoleId

                where ur.UserId == user.Id
                    && !rp.Permission.IsDelete
                    && rp.Permission.IsActive

                select rp.Permission.Code
            )
            .Distinct()
            .ToListAsync();

            DailyFileLogger.Info(
                $"User '{dto.Username}' authenticated successfully. " +
                $"Roles='{roles.Count}', Permissions='{permissions.Count}'."
            );

            DailyFileLogger.Audit(
                action: "LOGIN",
                entity: "USER",
                entityId: user.UserId,
                performedBy: user.UserId,
                description:
                    $"User login successful with {roles.Count} role(s) and {permissions.Count} permission(s)."
            );

            return new LoginResultDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Fullname = user.Fullname,   
                Roles = roles,
                Permissions = permissions
            };
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred during login validation for username '{dto.Username}'.",
                ex
            );

            throw;
        }
    }

    public async Task<string>
        GenerateTokenAsync(LoginResultDto user)
    {
        try
        {
            DailyFileLogger.Info(
                $"Generating JWT token for UserId '{user.UserId}'."
            );

            var entityUser = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == user.UserId
                );

            if (entityUser == null)
            {
                DailyFileLogger.Warn(
                    $"JWT generation failed because UserId '{user.UserId}' was not found."
                );

                throw new Exception(
                    "User not found."
                );
            }

            var token =
                await _jwtHelper.GenerateTokenAsync(
                    entityUser,
                    user.Permissions,
                    user.Roles
                );

            DailyFileLogger.Info(
                $"JWT token successfully generated for UserId '{user.UserId}'."
            );

            DailyFileLogger.Audit(
                action: "GENERATE_TOKEN",
                entity: "USER",
                entityId: entityUser.UserId,
                performedBy: entityUser.UserId,
                description:
                    "JWT token generated successfully."
            );

            return token;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while generating JWT token for UserId '{user.UserId}'.",
                ex
            );

            throw;
        }
    }
}