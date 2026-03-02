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
        return await _db.Permissions
            .Where(x => x.IsDelete == false)
            .Select(x => new PermissionResponseDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name
            })
            .ToListAsync();
    }

    public async Task<PermissionResponseDto?> GetByIdAsync(string id)
    {
        return await _db.Permissions
            .Where(x => x.Id == id && x.IsDelete == false)
            .Select(x => new PermissionResponseDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(PermissionDto dto, string createdBy)
    {
        var exists = await _db.Permissions
            .AnyAsync(x => x.Code == dto.Code && x.IsDelete == false);

        if (exists)
            throw new Exception("Permission code sudah ada.");

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
    }

    public async Task UpdateAsync(string id, PermissionDto dto, string updatedBy)
    {
        var permission = await _db.Permissions.FindAsync(id);

        if (permission == null || permission.IsDelete)
            throw new Exception("Permission tidak ditemukan.");

        permission.Code = dto.Code;
        permission.Name = dto.Name;
        //permission.UpdatedBy = updatedBy;
        //permission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var permission = await _db.Permissions.FindAsync(id);

        if (permission == null || permission.IsDelete)
            throw new Exception("Permission tidak ditemukan.");

        permission.IsDelete = true;
        //permission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}