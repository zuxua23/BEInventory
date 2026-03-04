namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class ItemService : IItemService
{
    private readonly AppDBContext _db;

    public ItemService(AppDBContext db)
    {
        _db = db;
    }

    // READ ALL
    public async Task<List<ItemResponseDto>> GetAllAsync()
    {
        try
        {
            var result = await _db.Items
                .Where(x => x.IsDelete == false)
                .Select(x => new ItemResponseDto
                {
                    Id = x.Id,
                    ItemId = x.ItmId,
                    ItemName = x.Name
                })
                .ToListAsync();

            DailyFileLogger.Info($"GetAllAsync Item berhasil, total {result.Count} data.");
            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di GetAllAsync Item.", ex);
            throw;
        }
    }

    // READ BY ID
    public async Task<ItemResponseDto?> GetByIdAsync(string id)
    {
        try
        {
            var item = await _db.Items
                .Where(x => x.Id == id && x.IsDelete == false)
                .Select(x => new ItemResponseDto
                {
                    Id = x.Id,
                    ItemId = x.ItmId,
                    ItemName = x.Name
                })
                .FirstOrDefaultAsync();

            if (item != null)
                DailyFileLogger.Info($"GetByIdAsync Item berhasil untuk ID {id}.");
            else
                DailyFileLogger.Warn($"GetByIdAsync Item: ID {id} tidak ditemukan.");

            return item;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di GetByIdAsync Item untuk ID {id}.", ex);
            throw;
        }
    }

    // CREATE
    public async Task CreateAsync(ItemDto dto, string createdBy)
    {
        try
        {
            var exists = await _db.Items
                .AnyAsync(x => x.ItmId == dto.ItemId && x.IsDelete == false);

            if (exists)
                throw new Exception("Item ID sudah digunakan.");

            var item = new Item
            {
                Id = Guid.NewGuid().ToString(),
                ItmId = dto.ItemId,
                Name = dto.ItemName,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            _db.Items.Add(item);
            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"CreateAsync Item berhasil untuk ItmId {dto.ItemId}.");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di CreateAsync Item untuk ItmId {dto.ItemId}.", ex);
            throw;
        }
    }

    // UPDATE
    public async Task UpdateAsync(string id, ItemDto dto, string updatedBy)
    {
        try
        {
            var item = await _db.Items.FindAsync(id);

            if (item == null || item.IsDelete == true)
                throw new Exception("Item tidak ditemukan");
            item.ItmId = dto.ItemId;
            item.Name = dto.ItemName;
            item.UpdatedBy = updatedBy;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"UpdateAsync Item berhasil untuk ID {id}.");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di UpdateAsync Item untuk ID {id}.", ex);
            throw;
        }
    }

    // DELETE (SOFT DELETE)
    public async Task DeleteAsync(string id)
    {
        try
        {
            var item = await _db.Items.FindAsync(id);

            if (item == null || item.IsDelete == true)
                throw new Exception("Item tidak ditemukan");

            item.IsDelete = true;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"DeleteAsync Item berhasil untuk ID {id} (soft delete).");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di DeleteAsync Item untuk ID {id}.", ex);
            throw;
        }
    }
}