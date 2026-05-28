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
                .Where(x => !x.IsDelete)
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

    public async Task CreateAsync( PickingListDTO request, string createdBy )
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

            var doDetailTags = new List<DODetailTag>();

            foreach (var requestDetail in request.Details)
            {
                var qtyRequired = requestDetail.QtyRequired ?? 0;

                if (qtyRequired <= 0)
                {
                    throw new Exception(
                        "Qty required must be greater than 0."
                    );
                }

                var availableTags = await _db.Tags
                    .Where(x =>
                        x.ItemId == requestDetail.ItemId &&
                        x.Status == TagStatus.IN_STOCK &&
                        !x.IsDelete
                    )
                    .OrderBy(x => x.TagId)
                    .Take(qtyRequired)
                    .ToListAsync();

                if (availableTags.Count < qtyRequired)
                {
                    var itemName = await _db.Items
                        .Where(x => x.Id == requestDetail.ItemId)
                        .Select(x => x.Name)
                        .FirstOrDefaultAsync();

                    throw new Exception(
                        $"Item '{itemName}' only has {availableTags.Count} available tag(s). Requested: {qtyRequired}."
                    );
                }
                var doDetail = new DODetail
                {
                    DoDetailId = Guid.NewGuid().ToString(),
                    DoId = doEntity.DoId,
                    ItemId = requestDetail.ItemId,
                    QtyRequired = qtyRequired
                };

                foreach (var tag in availableTags)
                {
                    tag.Status = TagStatus.ALLOCATED;
                    tag.UpdatedBy = createdBy;
                    tag.UpdatedAt = DateTime.UtcNow;

                    doDetailTags.Add(new DODetailTag
                    {
                        Id = Guid.NewGuid().ToString(),
                        DoDetailId = doDetail.DoDetailId,
                        TagId = tag.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                doDetails.Add(doDetail);
            }

            _db.DOs.Add(doEntity);

            _db.DODetails.AddRange(doDetails);

            _db.DODetailTags.AddRange(doDetailTags);

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

    public async Task UpdateAsync(string id, PickingListDTO dto,string updatedBy)
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
            // RELEASE OLD TAGS
            var oldRelations = await _db.DODetailTags
                .Include(x => x.Tag)
                .Include(x => x.DODetail)
                .Where(x => x.DODetail.DoId == id)
                .ToListAsync();

            foreach (var relation in oldRelations)
            {
                relation.Tag.Status = TagStatus.IN_STOCK;
                relation.Tag.UpdatedBy = updatedBy;
                relation.Tag.UpdatedAt = DateTime.UtcNow;
            }
            var oldDoNumber = doEntity.DoNumber;

            _db.DODetailTags.RemoveRange(oldRelations);

            _db.DODetails.RemoveRange(doEntity.Details);
            // UPDATE
            doEntity.DoNumber = dto.DoNumber;
            doEntity.UpdatedBy = updatedBy;
            doEntity.UpdatedAt = DateTime.UtcNow;

            var newDetails = new List<DODetail>();

            var newDetailTags = new List<DODetailTag>();

            foreach (var requestDetail in dto.Details)
            {
                var qtyRequired = requestDetail.QtyRequired ?? 0;

                if (qtyRequired <= 0)
                {
                    throw new Exception(
                        "Qty required must be greater than 0."
                    );
                }

                var availableTags = await _db.Tags
                    .Where(x =>
                        x.ItemId == requestDetail.ItemId &&
                        x.Status == TagStatus.IN_STOCK &&
                        !x.IsDelete
                    )
                    .OrderBy(x => x.TagId)
                    .Take(qtyRequired)
                    .ToListAsync();

                if (availableTags.Count < qtyRequired)
                {
                    var itemName = await _db.Items
                        .Where(x => x.Id == requestDetail.ItemId)
                        .Select(x => x.Name)
                        .FirstOrDefaultAsync();

                    throw new Exception(
                        $"Item '{itemName}' only has {availableTags.Count} available tag(s). Requested: {qtyRequired}."
                    );
                }


                var doDetail = new DODetail
                {
                    DoDetailId = Guid.NewGuid().ToString(),
                    DoId = doEntity.DoId,
                    ItemId = requestDetail.ItemId,
                    QtyRequired = qtyRequired
                };

                foreach (var tag in availableTags)
                {
                    tag.Status = TagStatus.ALLOCATED;
                    tag.UpdatedBy = updatedBy;
                    tag.UpdatedAt = DateTime.UtcNow;

                    newDetailTags.Add(new DODetailTag
                    {
                        Id = Guid.NewGuid().ToString(),
                        DoDetailId = doDetail.DoDetailId,
                        TagId = tag.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                newDetails.Add(doDetail);
            }

            _db.DODetails.AddRange(newDetails);

            _db.DODetailTags.AddRange(newDetailTags);

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

    public async Task DeleteAsync(string id, string deletedBy )
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

            // RELEASE TAGS
            var relations = await _db.DODetailTags
                .Include(x => x.Tag)
                .Include(x => x.DODetail)
                .Where(x => x.DODetail.DoId == id)
                .ToListAsync();
            var details = await _db.DODetails
                .Where(x => x.DoId == id)
                .ToListAsync();

            _db.DODetails.RemoveRange(details);

            foreach (var relation in relations)
            {
                relation.Tag.Status = TagStatus.IN_STOCK;
                relation.Tag.UpdatedBy = deletedBy;
                relation.Tag.UpdatedAt = DateTime.UtcNow;
            }

            _db.DODetailTags.RemoveRange(relations);

            doData.IsDelete = true;
            doData.UpdatedBy = deletedBy;
            doData.UpdatedAt = DateTime.UtcNow;

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
}