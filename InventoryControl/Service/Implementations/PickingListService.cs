namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Migrations;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class PickingListService : IPickingListService
{
    private readonly AppDBContext _db;

    public PickingListService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<List<DOResponseDto>> GetAllAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving all active delivery orders."
            );

            var result = await _db.DOs
                .Where(x => !x.IsDelete)
                .Include(x => x.Details)
                .ThenInclude(d => d.Item)
                .IgnoreQueryFilters()
                .Select(x => new DOResponseDto
                {
                    DoId = x.DoId,
                    DoNumber = x.DoNumber,
                    ScannerType = x.ScannerType,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    Details = x.Details.Select(d =>
                        new DODetailResponseDto
                        {
                            DoDetailId = d.DoDetailId,
                            ItemId = d.ItemId,
                            ItemName = d.Item.Name,
                            QtyRequired = d.QtyRequired
                        })
                        .ToList()
                })
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} delivery order(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving delivery orders.",
                ex
            );

            throw;
        }
    }

    public async Task<DO?> GetByIdAsync(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving delivery order with ID '{id}'."
            );

            var result = await _db.DOs
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x =>
                    x.DoId == id &&
                    !x.IsDelete
                );

            if (result == null)
            {
                DailyFileLogger.Warn(
                    $"Delivery order with ID '{id}' was not found."
                );

                return null;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved delivery order with ID '{id}'."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving delivery order with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task CreateAsync(
        PickingListDTO request,
        string createdBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Creating new delivery order with number '{request.DoNumber}'.",
                createdBy
            );

            var doExists = await _db.DOs
                .AnyAsync(x =>
                    x.DoNumber == request.DoNumber &&
                    !x.IsDelete
                );

            if (doExists)
            {
                DailyFileLogger.Warn(
                    $"Delivery order number '{request.DoNumber}' already exists.",
                    createdBy
                );

                throw new Exception(
                    "Delivery order number already exists."
                );
            }

            var doEntity = new DO
            {
                DoId = Guid.NewGuid().ToString(),
                DoNumber = request.DoNumber,
                Status = "DRAFT",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            var details = request.Details
                .Select(d => new DODetail
                {
                    DoDetailId = Guid.NewGuid().ToString(),
                    DoId = doEntity.DoId,
                    ItemId = d.ItemId,
                    QtyRequired = d.QtyRequired
                })
                .ToList();

            _db.DOs.Add(doEntity);

            _db.DODetails.AddRange(details);

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Delivery order successfully created with ID '{doEntity.DoId}'.",
                createdBy
            );

            DailyFileLogger.Audit(
                action: "CREATE",
                entity: "DELIVERY_ORDER",
                entityId: doEntity.DoNumber,
                performedBy: createdBy,
                description:
                    $"Created delivery order with {details.Count} item(s)."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while creating delivery order.",
                ex,
                createdBy
            );

            throw;
        }
    }

    public async Task UpdateAsync(
        string id,
        PickingListDTO dto,
        string updatedBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating delivery order with ID '{id}'."
            );

            var doEntity = await _db.DOs
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x =>
                    x.DoId == id &&
                    !x.IsDelete
                );

            if (doEntity == null)
            {
                DailyFileLogger.Warn(
                    $"Update failed. Delivery order with ID '{id}' was not found."
                );

                throw new Exception(
                    "Delivery order not found."
                );
            }

            if (doEntity.Status != "DRAFT")
            {
                DailyFileLogger.Warn(
                    $"Update denied. Delivery order with ID '{id}' is not in DRAFT status."
                );

                throw new Exception(
                    "Delivery order can only be updated in DRAFT status."
                );
            }

            var oldDoNumber = doEntity.DoNumber;

            doEntity.DoNumber = dto.DoNumber;
            doEntity.UpdatedBy = updatedBy;
            doEntity.UpdatedAt = DateTime.UtcNow;

            _db.DODetails.RemoveRange(
                doEntity.Details
            );

            var newDetails = dto.Details
                .Select(d => new DODetail
                {
                    DoDetailId = Guid.NewGuid().ToString(),
                    DoId = doEntity.DoId,
                    ItemId = d.ItemId,
                    QtyRequired = d.QtyRequired
                })
                .ToList();

            _db.DODetails.AddRange(newDetails);

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Delivery order successfully updated. ID='{id}'."
            );

            DailyFileLogger.Audit(
                action: "UPDATE",
                entity: "DELIVERY_ORDER",
                entityId: doEntity.DoNumber,
                performedBy: updatedBy,
                description:
                    $"Updated delivery order from Number='{oldDoNumber}' to Number='{dto.DoNumber}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while updating delivery order with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task DeleteAsync(
        string id,
        string deletedBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Deleting delivery order with ID '{id}'.",
                deletedBy
            );

            var doData = await _db.DOs
                .FirstOrDefaultAsync(x =>
                    x.DoId == id &&
                    !x.IsDelete
                );

            if (doData == null)
            {
                DailyFileLogger.Warn(
                    $"Delete failed. Delivery order with ID '{id}' was not found.",
                    deletedBy
                );

                throw new Exception(
                    "Delivery order not found."
                );
            }

            doData.IsDelete = true;
            doData.UpdatedBy = deletedBy;
            doData.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Delivery order successfully soft deleted. ID='{id}'.",
                deletedBy
            );

            DailyFileLogger.Audit(
                action: "DELETE",
                entity: "DELIVERY_ORDER",
                entityId: doData.DoNumber,
                performedBy: deletedBy,
                description:
                    $"Soft deleted delivery order '{doData.DoNumber}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while deleting delivery order with ID '{id}'.",
                ex,
                deletedBy
            );

            throw;
        }
    }
}