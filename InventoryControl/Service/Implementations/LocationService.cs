namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
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

    public async Task CreateAsync(LocationDTO dto, string createdBy)
    {
        try
        {
            var location = new Location
            {
                Id = Guid.NewGuid().ToString(),
                LocId = dto.LocId,
                Name = dto.Name,
                Description = dto.Description,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.Locations.Add(location);
            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"CreateAsync berhasil untuk LocationID {dto.LocId}.");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di CreateAsync untuk LocationID {dto.LocId}.", ex);
            throw;
        }
    }

    public async Task UpdateAsync(string id, LocationDTO dto, string updatedBy)
    {
        try
        {
            var location = await _db.Locations.FindAsync(id);

            if (location == null || location.IsDelete)
            {
                DailyFileLogger.Warn($"UpdateAsync: Location dengan ID {id} tidak ditemukan.");
                throw new Exception("Location tidak ditemukan");
            }

            location.LocId = dto.LocId;
            location.Name = dto.Name;
            location.Description = dto.Description;
            location.UpdatedBy = updatedBy;
            location.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"UpdateAsync berhasil untuk ID {id}.");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di UpdateAsync untuk ID {id}.", ex);
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var location = await _db.Locations.FindAsync(id);

            if (location == null || location.IsDelete)
            {
                DailyFileLogger.Warn($"DeleteAsync: Location dengan ID {id} tidak ditemukan.");
                throw new Exception("Location tidak ditemukan");

            }

            location.IsDelete = true;
            location.UpdatedAt = DateTime.UtcNow;

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