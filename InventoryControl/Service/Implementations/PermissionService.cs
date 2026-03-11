namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class PermissionService : IPermissionService
{
    private readonly AppDBContext _db;

    public PermissionService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<List<PermissionResponseDto>> GetAllAsync()
    {
        try
        {
            var result = await _db.Permissions
                .Where(x => x.IsDelete == false)
                .Select(x => new PermissionResponseDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name
                })
                .ToListAsync();

            DailyFileLogger.Info($"GetAll Permission berhasil. Total={result.Count}");

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di PermissionService.GetAllAsync", ex);
            throw;
        }
    }

    public async Task<PermissionResponseDto?> GetByIdAsync(string id)
    {
        try
        {
            var permission = await _db.Permissions
                .Where(x => x.Id == id && x.IsDelete == false)
                .Select(x => new PermissionResponseDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name
                })
                .FirstOrDefaultAsync();

            if (permission == null)
                DailyFileLogger.Warn($"GetById Permission gagal. Id={id} tidak ditemukan");
            else
                DailyFileLogger.Info($"GetById Permission berhasil. Id={id}");

            return permission;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di PermissionService.GetByIdAsync. Id={id}", ex);
            throw;
        }
    }

    public async Task CreateAsync(PermissionDto dto, string createdBy)
    {
        try
        {
            var exists = await _db.Permissions
                .AnyAsync(x => x.Code == dto.Code && x.IsDelete == false);

            if (exists)
            {
                DailyFileLogger.Warn($"Create Permission gagal. Code={dto.Code} sudah ada");
                throw new Exception("Permission code sudah ada.");
            }

            var permission = new Permission
            {
                Id = Guid.NewGuid().ToString(),
                Code = dto.Code,
                Name = dto.Name,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"Permission berhasil dibuat. Code={dto.Code}, User={createdBy}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di PermissionService.CreateAsync. Code={dto.Code}", ex);
            throw;
        }
    }

    public async Task UpdateAsync(string id, PermissionUpdateDto dto, string updatedBy)
    {
        try
        {
            var permission = await _db.Permissions.FindAsync(id);

            if (permission == null || permission.IsDelete)
            {
                DailyFileLogger.Warn($"Update Permission gagal. Id={id} tidak ditemukan");
                throw new Exception("Permission tidak ditemukan.");
            }

            permission.Code = dto.Code;
            permission.Name = dto.Name;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"Permission berhasil diupdate. Id={id}, User={updatedBy}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di PermissionService.UpdateAsync. Id={id}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var permission = await _db.Permissions.FindAsync(id);

            if (permission == null || permission.IsDelete)
            {
                DailyFileLogger.Warn($"Delete Permission gagal. Id={id} tidak ditemukan");
                throw new Exception("Permission tidak ditemukan.");
            }

            permission.IsDelete = true;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"Permission berhasil dihapus (soft delete). Id={id}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di PermissionService.DeleteAsync. Id={id}", ex);
            throw;
        }
    }
}