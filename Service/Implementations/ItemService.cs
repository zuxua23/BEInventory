namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
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

    public async Task<List<ItemResponseDto>> GetAllAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving all active items."
            );

            var result = await _db.Items
                .Where(x => !x.IsDelete)
                .Select(x => new ItemResponseDto
                {
                    Id = x.Id,
                    ItemId = x.ItmId,
                    ItemName = x.Name,
                    ItemDesc = x.Description,
                    MinimumStock = x.MinimumStock
                })
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} active item(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving all items.",
                ex
            );

            throw;
        }
    }

    public async Task<ItemResponseDto?> GetByIdAsync(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving item detail for ID '{id}'."
            );

            var item = await _db.Items
                .Where(x => x.Id == id && x.IsDelete == false)
                .Select(x => new ItemResponseDto
                {
                    Id = x.Id,
                    ItemId = x.ItmId,
                    ItemName = x.Name,
                    ItemDesc = x.Description,
                    MinimumStock = x.MinimumStock
                })
                .FirstOrDefaultAsync();

            if (item == null)
            {
                DailyFileLogger.Warn(
                    $"Item with ID '{id}' was not found."
                );

                return null;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved item with ID '{id}'."
            );

            return item;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving item with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task CreateAsync(ItemDto dto, string createdBy)
    {
        try
        {
            DailyFileLogger.Info(
                $"Creating new item with name '{dto.ItemName}'.",
                createdBy
            );

            if ( dto.MinimumStock < 0)
            {
                throw new Exception("Stock threshold cannot be negative.");
            }

            var lastItem = await _db.Items
                 .IgnoreQueryFilters()
                .OrderByDescending(x => x.ItmId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastItem != null)
            {
                var lastNumber = int.Parse(
                    lastItem.ItmId.Replace("ITM", "")
                );

                nextNumber = lastNumber + 1;
            }

            string newItemId =
                "ITM" + nextNumber.ToString("D5");

            var exists = await _db.Items
                .AnyAsync(x =>
                    x.ItmId == newItemId &&
                    x.IsDelete == false
                );

            if (exists)
            {
                DailyFileLogger.Warn(
                    $"Generated item ID '{newItemId}' already exists.",
                    createdBy
                );

                throw new Exception(
                    "Generated item ID already exists."
                );
            }

            var item = new Item
            {
                Id = Guid.NewGuid().ToString(),
                ItmId = newItemId,
                Name = dto.ItemName,
                Description = dto.ItemDesc,
                MinimumStock = dto.MinimumStock,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            _db.Items.Add(item);

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Item successfully created with ItemId '{newItemId}'.",
                createdBy
            );

            DailyFileLogger.Audit(
                action: "CREATE",
                entity: "ITEM",
                entityId: newItemId,
                performedBy: createdBy,
                description:
                    $"Created item '{dto.ItemName}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while creating a new item.",
                ex,
                createdBy
            );

            throw;
        }
    }

    public async Task UpdateAsync(
        string id,
        ItemDto dto,
        string updatedBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating item with ID '{id}'.",
                updatedBy
            );

            var item = await _db.Items.FindAsync(id);

            if (item == null || item.IsDelete)
            {
                DailyFileLogger.Warn(
                    $"Update failed. Item with ID '{id}' was not found.",
                    updatedBy
                );

                throw new Exception(
                    "Item not found."
                );
            }

            if (dto.MinimumStock < 0)
            {
                throw new Exception("Stock threshold cannot be negative.");
            }

            var oldName = item.Name;

            item.Name = dto.ItemName;
            item.Description = dto.ItemDesc;
            item.MinimumStock = dto.MinimumStock;
            item.UpdatedBy = updatedBy;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Item successfully updated. ID='{id}'.",
                updatedBy
            );

            DailyFileLogger.Audit(
                action: "UPDATE",
                entity: "ITEM",
                entityId: item.ItmId,
                performedBy: updatedBy,
                description:
                    $"Updated item name from '{oldName}' to '{dto.ItemName}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while updating item with ID '{id}'.",
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
                $"Deleting item with ID '{id}'.",
                deletedBy
            );

            var item = await _db.Items.FindAsync(id);

            if (item == null || item.IsDelete)
            {
                DailyFileLogger.Warn(
                    $"Delete failed. Item with ID '{id}' was not found.",
                    deletedBy
                );

                throw new Exception("Item not found.");
            }

            item.IsDelete = true;
            item.UpdatedBy = deletedBy;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Item successfully soft deleted. ID='{id}'.",
                deletedBy
            );

            DailyFileLogger.Audit(
                action: "DELETE",
                entity: "ITEM",
                entityId: item.ItmId,
                performedBy: deletedBy,
                description:
                    $"Soft deleted item '{item.Name}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while deleting item with ID '{id}'.",
                ex,
                deletedBy
            );

            throw;
        }
    }


    public async Task<int?> GetAvailableStockAsync(string itemId)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving item stock for ID '{itemId}'."
            );
            return await _db.Tags
            .CountAsync(x =>
                x.ItemId == itemId &&
                x.Status == TagStatus.IN_STOCK
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving item with ID '{itemId}'.",
                ex
            );

            throw;
        }
    }
}
