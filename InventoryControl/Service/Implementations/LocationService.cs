namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

public class LocationService : ILocationService
{
    private readonly AppDBContext _db;

    public LocationService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<List<Location>> GetAllAsync()
    {
        return await _db.Locations
            .Where(x => !x.IsDelete)
            .ToListAsync();
    }

    public async Task<Location?> GetByIdAsync(string id)
    {
        return await _db.Locations
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDelete);
    }

    public async Task CreateAsync(Location dto, string createdBy)
    {
        dto.Id = Guid.NewGuid().ToString();
        dto.CreatedBy = createdBy;
        dto.CreatedAt = DateTime.UtcNow;
        dto.IsDelete = false;

        _db.Locations.Add(dto);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(string id, Location dto, string updatedBy)
    {
        var location = await _db.Locations.FindAsync(id);

        if (location == null || location.IsDelete)
            throw new Exception("Location tidak ditemukan");

        location.LocId = dto.LocId;
        location.name = dto.name;
        location.Description = dto.Description;
        location.UpdatedBy = updatedBy;
        location.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var location = await _db.Locations.FindAsync(id);

        if (location == null || location.IsDelete)
            throw new Exception("Location tidak ditemukan");

        location.IsDelete = true;
        location.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}