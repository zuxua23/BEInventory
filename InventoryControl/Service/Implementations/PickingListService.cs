namespace InventoryControl.Service.Implementations;

using Azure.Core;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
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
             .AsNoTracking()
             .Where(x => !x.IsDelete)
             .Select(x => new DOResponseDto
             {
                 DoId = x.DoId,
                 DoNumber = x.DoNumber,
                 ScannerType = x.ScannerType,
                 Status = x.Status.ToString(),
                 CreatedAt = x.CreatedAt
             })
             .OrderByDescending(x => x.CreatedAt)
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

    public async Task<List<DOResponseDto>> GetDoItemAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving all delivery order items."
            );

            var result = await _db.DOs
                .Where(x =>
                    !x.IsDelete && x.Status == DoStatus.PREPARATION)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Item)
                .Include(x => x.Details)
                    .ThenInclude(d => d.DODetailTags)
                        .ThenInclude(dt => dt.Tag)
                .IgnoreQueryFilters()
                .Select(x => new DOResponseDto
                {
                    DoId = x.DoId,
                    DoNumber = x.DoNumber,
                    ScannerType = x.ScannerType,
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt,
                    Details = x.Details.Select(d =>
                        new DODetailResponseDto
                        {
                            DoDetailId = d.DoDetailId,
                            ItemId = d.ItemId,
                            ItemName = d.Item.Name,
                            QtyRequired = d.QtyRequired,
                            Tags = d.DODetailTags.Select(x => new DOTagResponseDto
                            {
                                TagId = x.Tag.TagId,
                                EpcTag = x.Tag.EpcTag
                            }).ToList()
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


    public async Task<DOStatusCountDto> GetDOStatusCountAsync()
    {
        try
        {
            DailyFileLogger.Info("Retrieving delivery order count by status.");

            var data = await _db.DOs
                .AsNoTracking()
                .Where(x => !x.IsDelete)
                .GroupBy(x => 1)
                .Select(g => new DOStatusCountDto
                {
                    Draft = g.Count(x => x.Status == DoStatus.DRAFT),

                    Preparation = g.Count(x => x.Status == DoStatus.PREPARATION),

                    Completed = g.Count(x => x.Status == DoStatus.COMPLETED),

                    Active = g.Count(x =>
                        x.Status == DoStatus.DRAFT ||
                        x.Status == DoStatus.PREPARATION),

                    Total = g.Count()
                })
                .FirstOrDefaultAsync();

            return data ?? new DOStatusCountDto();
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving delivery order count by status.",
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
                    .ThenInclude(d => d.Item)

                .Include(x => x.Details)
                    .ThenInclude(d => d.DODetailTags)
                        .ThenInclude(dt => dt.Tag)

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

    public async Task CreateAsync(PickingListDTO request, string createdBy)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

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
                Status = DoStatus.DRAFT,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };
            var doDetails = new List<DODetail>();

            foreach (var requestDetail in request.Details)
            {
                var qtyRequired = requestDetail.QtyRequired ?? 0;

                if (qtyRequired <= 0)
                {
                    throw new Exception("Qty required must be greater than 0.");
                }

                var itemName = await _db.Items
                    .Where(x => x.Id == requestDetail.ItemId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();

                if (itemName == null)
                {
                    throw new Exception("Item not found.");
                }

                var availableStock = await _db.Tags
                    .CountAsync(x =>
                        x.ItemId == requestDetail.ItemId &&
                        x.Status == TagStatus.IN_STOCK &&
                        !x.IsDelete
                    );

                if (availableStock < qtyRequired)
                {
                    throw new Exception(
                        $"Item '{itemName}' only has {availableStock} available tag(s). Requested: {qtyRequired}."
                    );
                }

                doDetails.Add(new DODetail
                {
                    DoDetailId = Guid.NewGuid().ToString(),
                    DoId = doEntity.DoId,
                    ItemId = requestDetail.ItemId,
                    QtyRequired = qtyRequired
                });
            }


            _db.DOs.Add(doEntity);
            _db.DODetails.AddRange(doDetails);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

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
                    $"Created delivery order with {doDetails.Count} item(s)."
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            DailyFileLogger.Error(
                "An error occurred while creating delivery order.",
                ex,
                createdBy
            );

            throw;
        }
    }

    public async Task UpdateAsync(string id, PickingListDTO dto, string updatedBy)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            DailyFileLogger.Info(
                $"Updating delivery order with ID '{id}'."
            );

            var doEntity = await _db.DOs
                  .Include(x => x.Details)
                  .ThenInclude(d => d.DODetailTags)
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

            if (doEntity.Status != DoStatus.DRAFT)
            {
                DailyFileLogger.Warn(
                    $"Update denied. Delivery order with ID '{id}' is not in DRAFT status."
                );

                throw new Exception(
                    "Delivery order can only be updated in DRAFT status."
                );
            }
            var doNumberExists = await _db.DOs
              .AnyAsync(x =>
                  x.DoNumber == dto.DoNumber &&
                  x.DoId != id &&
                  !x.IsDelete
              );

            if (doNumberExists)
            {
                throw new Exception("Delivery order number already exists.");
            }

            var oldDoNumber = doEntity.DoNumber;

            _db.DODetails.RemoveRange(doEntity.Details);

            doEntity.DoNumber = dto.DoNumber;
            doEntity.UpdatedBy = updatedBy;
            doEntity.UpdatedAt = DateTime.UtcNow;

            var newDetails = new List<DODetail>();

            foreach (var requestDetail in dto.Details)
            {
                var qtyRequired = requestDetail.QtyRequired ?? 0;

                if (qtyRequired <= 0)
                {
                    throw new Exception("Qty required must be greater than 0.");
                }

                var itemName = await _db.Items
                    .Where(x => x.Id == requestDetail.ItemId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();

                if (itemName == null)
                {
                    throw new Exception("Item not found.");
                }

                var availableStock = await _db.Tags
                    .CountAsync(x =>
                        x.ItemId == requestDetail.ItemId &&
                        x.Status == TagStatus.IN_STOCK &&
                        !x.IsDelete
                    );

                if (availableStock < qtyRequired)
                {
                    throw new Exception(
                        $"Item '{itemName}' only has {availableStock} available tag(s). Requested: {qtyRequired}."
                    );
                }

                newDetails.Add(new DODetail
                {
                    DoDetailId = Guid.NewGuid().ToString(),
                    DoId = doEntity.DoId,
                    ItemId = requestDetail.ItemId,
                    QtyRequired = qtyRequired
                });
            }

            _db.DODetails.AddRange(newDetails);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            DailyFileLogger.Info(
                $"Delivery order successfully updated. ID='{id}'."
            );

            DailyFileLogger.Audit(
                action: "UPDATE",
                entity: "DELIVERY_ORDER",
                entityId: doEntity.DoNumber,
                performedBy: updatedBy,
                description:
                    $"Updated delivery order from Number='{oldDoNumber}' to '{dto.DoNumber}'.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            DailyFileLogger.Error(
                $"An error occurred while updating delivery order with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task DeleteAsync(string id, string deletedBy)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

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

            if (doData.Status != DoStatus.DRAFT)
            {
                DailyFileLogger.Warn(
                    $"Delete failed. Delivery order '{doData.DoNumber}' cannot be deleted because status is '{doData.Status}'.",
                    deletedBy
                );

                throw new Exception("Only draft delivery order can be deleted.");
            }

            var now = DateTime.UtcNow;

            var details = await _db.DODetails
                .Where(x => x.DoId == id)
                .ToListAsync();

            _db.DODetails.RemoveRange(details);

            doData.IsDelete = true;
            doData.UpdatedBy = deletedBy;
            doData.UpdatedAt = now;

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

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
            await transaction.RollbackAsync();

            DailyFileLogger.Error(
                $"An error occurred while deleting delivery order with ID '{id}'.",
                ex,
                deletedBy
            );

            throw;
        }
    }

    public async Task<int> GetAvailableStockForEditAsync(string itemId, string? doId)
    {
        return await _db.Tags
            .CountAsync(x =>
                x.ItemId == itemId &&
                x.Status == TagStatus.IN_STOCK &&
                !x.IsDelete
            );
    }
}