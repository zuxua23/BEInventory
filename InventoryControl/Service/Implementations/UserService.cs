namespace InventoryControl.Service.Implementations;

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
            DailyFileLogger.Info(
                "Retrieving all active users."
            );

            var result = await _db.Users
                .Where(x => !x.IsDelete)
                .Select(x => new UserResponseDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Fullname = x.Fullname,
                    Username = x.Username,

                    Roles = _db.UserRoles
                        .Where(ur =>
                            ur.UserId == x.Id
                        )
                        .Include(ur => ur.Role)
                        .Select(ur =>
                            ur.Role.Name
                        )
                        .ToList()
                })
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} active user(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving users.",
                ex
            );

            throw;
        }
    }

    public async Task<UserResponseDto?> GetByIdAsync(
        string id
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving user detail for ID '{id}'."
            );

            var user = await _db.Users
                .Where(x =>
                    x.Id == id &&
                    !x.IsDelete
                )
                .Select(x => new UserResponseDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Fullname = x.Fullname,
                    Username = x.Username,

                    Roles = _db.UserRoles
                        .Where(ur =>
                            ur.UserId == x.Id
                        )
                        .Include(ur => ur.Role)
                        .Select(ur =>
                            ur.Role.Name
                        )
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                DailyFileLogger.Warn(
                    $"User with ID '{id}' was not found."
                );

                return null;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved user with ID '{id}'."
            );

            return user;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving user with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task CreateAsync(
        UserDto dto,
        string createdBy
    )
    {
        using var transaction =
            await _db.Database.BeginTransactionAsync();

        try
        {
            DailyFileLogger.Info(
                $"Creating new user with username '{dto.Username}'.",
                createdBy
            );

            var lastUser = await _db.Users
                .IgnoreQueryFilters()
                .OrderByDescending(x =>
                    x.UserId
                )
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastUser != null)
            {
                var lastNumber = int.Parse(
                    lastUser.UserId.Replace(
                        "USR",
                        ""
                    )
                );

                nextNumber = lastNumber + 1;
            }

            string newUserId =
                "USR" +
                nextNumber.ToString("D5");

            if (
                string.IsNullOrWhiteSpace(
                    dto.Password
                )
            )
            {
                DailyFileLogger.Warn(
                    "User creation failed because password is empty.",
                    createdBy
                );

                throw new Exception(
                    "Password cannot be empty."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Username
                )
            )
            {
                DailyFileLogger.Warn(
                    "User creation failed because username is empty.",
                    createdBy
                );

                throw new Exception(
                    "Username cannot be empty."
                );
            }

            var usernameExists =
                await _db.Users.AnyAsync(x =>
                    x.Username == dto.Username &&
                    !x.IsDelete
                );

            if (usernameExists)
            {
                DailyFileLogger.Warn(
                    $"Username '{dto.Username}' already exists.",
                    createdBy
                );

                throw new Exception(
                    "Username already exists."
                );
            }

            var hashedPassword =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.Password
                );

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserId = newUserId,
                Fullname = dto.Fullname,
                Username = dto.Username,
                Password = hashedPassword,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            if (
                dto.RoleIds != null &&
                dto.RoleIds.Any()
            )
            {
                var validRoles =
                    await _db.Roles
                        .Where(r =>
                            dto.RoleIds.Contains(
                                r.Id
                            ) &&
                            !r.IsDelete
                        )
                        .Select(r => r.Id)
                        .ToListAsync();

                var userRoles =
                    validRoles.Select(roleId =>
                        new User_Role
                        {
                            Id = Guid.NewGuid()
                                .ToString(),

                            UserId = user.Id,
                            RoleId = roleId,

                            CreatedBy = createdBy,
                            CreatedAt =
                                DateTime.UtcNow
                        });

                await _db.UserRoles
                    .AddRangeAsync(userRoles);

                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            DailyFileLogger.Info(
                $"User successfully created with UserId '{newUserId}'.",
                createdBy
            );

            DailyFileLogger.Audit(
                action: "CREATE",
                entity: "USER",
                entityId: newUserId,
                performedBy: createdBy,
                description:
                    $"Created user '{dto.Username}'."
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            DailyFileLogger.Error(
                "An error occurred while creating user.",
                ex,
                createdBy
            );

            throw;
        }
    }

    public async Task UpdateAsync(
        string id,
        UpdateUserDto dto,
        string updatedBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating user with ID '{id}'.",
                updatedBy
            );

            var user = await _db.Users
                .FindAsync(id);

            if (
                user == null ||
                user.IsDelete
            )
            {
                DailyFileLogger.Warn(
                    $"User with ID '{id}' was not found.",
                    updatedBy
                );

                throw new Exception(
                    "User not found."
                );
            }

            var oldFullname =
                user.Fullname;

            var oldUsername =
                user.Username;

            user.Fullname =
                dto.Fullname;

            user.Username =
                dto.Username;

            user.UpdatedBy =
                updatedBy;

            user.UpdatedAt =
                DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"User successfully updated. ID='{id}'.",
                updatedBy
            );

            DailyFileLogger.Audit(
                action: "UPDATE",
                entity: "USER",
                entityId: user.UserId,
                performedBy: updatedBy,
                description:
                    $"Updated user from Fullname='{oldFullname}', Username='{oldUsername}' " +
                    $"to Fullname='{dto.Fullname}', Username='{dto.Username}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while updating user with ID '{id}'.",
                ex,
                updatedBy
            );

            throw;
        }
    }

    public async Task UpdateUserRolesAsync(
        UpdateUserRoleDto dto,
        string updatedBy
    )
    {
        using var trx =
            await _db.Database.BeginTransactionAsync();

        try
        {
            DailyFileLogger.Info(
                $"Updating roles for UserId '{dto.UserId}'.",
                updatedBy
            );

            var userExists =
                await _db.Users.AnyAsync(x =>
                    x.Id == dto.UserId &&
                    !x.IsDelete
                );

            if (!userExists)
            {
                DailyFileLogger.Warn(
                    $"User with ID '{dto.UserId}' was not found.",
                    updatedBy
                );

                throw new Exception(
                    "User not found."
                );
            }

            var oldRoles =
                _db.UserRoles.Where(x =>
                    x.UserId == dto.UserId
                );

            _db.UserRoles.RemoveRange(
                oldRoles
            );

            if (
                dto.Roles != null &&
                dto.Roles.Any()
            )
            {
                var validRoles =
                    await _db.Roles
                        .Where(r =>
                            dto.Roles.Contains(
                                r.Id
                            ) &&
                            !r.IsDelete
                        )
                        .Select(r => r.Id)
                        .ToListAsync();

                var userRoles =
                    validRoles.Select(roleId =>
                        new User_Role
                        {
                            Id = Guid.NewGuid()
                                .ToString(),

                            UserId = dto.UserId,
                            RoleId = roleId,

                            CreatedBy = updatedBy,
                            CreatedAt =
                                DateTime.UtcNow
                        });

                await _db.UserRoles
                    .AddRangeAsync(userRoles);
            }

            await _db.SaveChangesAsync();

            await trx.CommitAsync();

            DailyFileLogger.Info(
                $"Roles successfully updated for UserId '{dto.UserId}'.",
                updatedBy
            );

            DailyFileLogger.Audit(
                action: "UPDATE_ROLE",
                entity: "USER",
                entityId: dto.UserId,
                performedBy: updatedBy,
                description:
                    $"Updated user role assignments."
            );
        }
        catch (Exception ex)
        {
            await trx.RollbackAsync();

            DailyFileLogger.Error(
                $"An error occurred while updating roles for UserId '{dto.UserId}'.",
                ex,
                updatedBy
            );

            throw;
        }
    }

    public async Task UpdatePasswordAsync(
        string id,
        UpdatePasswordDto dto,
        string updatedBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating password for UserId '{id}'.",
                updatedBy
            );

            var user = await _db.Users
                .FindAsync(id);

            if (
                user == null ||
                user.IsDelete
            )
            {
                DailyFileLogger.Warn(
                    $"User with ID '{id}' was not found.",
                    updatedBy
                );

                throw new Exception(
                    "User not found."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Password
                )
            )
            {
                DailyFileLogger.Warn(
                    "Password update failed because password is empty.",
                    updatedBy
                );

                throw new Exception(
                    "Password cannot be empty."
                );
            }

            user.Password =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.Password
                );

            user.UpdatedBy =
                updatedBy;

            user.UpdatedAt =
                DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Password successfully updated for UserId '{id}'.",
                updatedBy
            );

            DailyFileLogger.Audit(
                action: "UPDATE_PASSWORD",
                entity: "USER",
                entityId: user.UserId,
                performedBy: updatedBy,
                description:
                    $"Updated password for user '{user.Username}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while updating password for UserId '{id}'.",
                ex,
                updatedBy
            );

            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Deleting user with ID '{id}'."
            );

            var user = await _db.Users
                .FindAsync(id);

            if (user == null)
            {
                DailyFileLogger.Warn(
                    $"User with ID '{id}' was not found."
                );

                throw new Exception(
                    "User not found."
                );
            }

            user.IsDelete = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"User successfully soft deleted. ID='{id}'."
            );

            DailyFileLogger.Audit(
                action: "DELETE",
                entity: "USER",
                entityId: user.UserId,
                performedBy: user.UpdatedBy ?? "SYSTEM",
                description:
                    $"Soft deleted user '{user.Username}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while deleting user with ID '{id}'.",
                ex
            );

            throw;
        }
    }
}