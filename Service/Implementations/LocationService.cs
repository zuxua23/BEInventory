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
        try
        {
            DailyFileLogger.Info(
                "Retrieving all active locations."
            );

            var result = await _db.Locations
                .Where(x => !x.IsDelete)
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} active location(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving all locations.",
                ex
            );

            throw;
        }
    }

    public async Task<Location?> GetByIdAsync(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving location detail for ID '{id}'."
            );

            var location = await _db.Locations
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.IsDelete
                );

            if (location == null)
            {
                DailyFileLogger.Warn(
                    $"Location with ID '{id}' was not found."
                );

                return null;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved location with ID '{id}'."
            );

            return location;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving location with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task CreateAsync(
        LocationDTO dto,
        string createdBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Creating new location with LocationId '{dto.LocId}'.",
                createdBy
            );

            var locationExists = await _db.Locations
                .AnyAsync(x =>
                    x.LocId == dto.LocId &&
                    !x.IsDelete
                );

            if (locationExists)
            {
                DailyFileLogger.Warn(
                    $"Location ID '{dto.LocId}' already exists.",
                    createdBy
                );

                throw new Exception(
                    "Location ID already exists."
                );
            }

            var location = new Location
            {
                Id = Guid.NewGuid().ToString(),
                LocId = dto.LocId,
                Name = dto.Name,
                Description = dto.Description,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            _db.Locations.Add(location);

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Location successfully created with LocationId '{dto.LocId}'.",
                createdBy
            );

            DailyFileLogger.Audit(
                action: "CREATE",
                entity: "LOCATION",
                entityId: dto.LocId,
                performedBy: createdBy,
                description:
                    $"Created location '{dto.Name}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while creating location '{dto.LocId}'.",
                ex,
                createdBy
            );

            throw;
        }
    }

    public async Task UpdateAsync(
        string id,
        LocationDTO dto,
        string updatedBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating location with ID '{id}'.",
                updatedBy
            );

            var location = await _db.Locations
                .FindAsync(id);

            if (location == null || location.IsDelete)
            {
                DailyFileLogger.Warn(
                    $"Update failed. Location with ID '{id}' was not found.",
                    updatedBy
                );

                throw new Exception(
                    "Location not found."
                );
            }

            if (location.IsSystem)
                throw new Exception("System location cannot be updated");

            var oldLocId = location.LocId;
            var oldName = location.Name;
            var oldDescription = location.Description;

            location.LocId = dto.LocId;
            location.Name = dto.Name;
            location.Description = dto.Description;
            location.UpdatedBy = updatedBy;
            location.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Location successfully updated. ID='{id}'.",
                updatedBy
            );

            DailyFileLogger.Audit(
                action: "UPDATE",
                entity: "LOCATION",
                entityId: location.LocId,
                performedBy: updatedBy,
                description:
                    $"Updated location from " +
                    $"LocId='{oldLocId}', Name='{oldName}', Description='{oldDescription}' " +
                    $"to LocId='{dto.LocId}', Name='{dto.Name}', Description='{dto.Description}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while updating location with ID '{id}'.",
                ex,
                updatedBy
            );

            throw;
        }
    }

    public async Task DeleteAsync(string id, string deletedBy)
    {
        try
        {
            DailyFileLogger.Info(
                $"Deleting location with ID '{id}'.",
                deletedBy
            );

            var location = await _db.Locations
                .FindAsync(id);

            if (location == null || location.IsDelete)
            {
                DailyFileLogger.Warn(
                    $"Delete failed. Location with ID '{id}' was not found."
                );

                throw new Exception(
                    "Location not found."
                );
            }
            if (location.IsSystem)
                throw new Exception("System location cannot be deleted");

            location.IsDelete = true;
            location.UpdatedAt = DateTime.UtcNow;
            location.UpdatedBy = deletedBy;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Location successfully soft deleted. ID='{id}'."
            );

            DailyFileLogger.Audit(
                action: "DELETE",
                entity: "LOCATION",
                entityId: location.LocId,
                performedBy: deletedBy,
                description:
                    $"Soft deleted location '{location.Name}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while deleting location with ID '{id}'.",
                ex
            );

            throw;
        }
    }
}