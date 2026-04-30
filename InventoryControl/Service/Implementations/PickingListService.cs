namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
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
            var result= await _db.DOs
            .Include(x => x.Details)
            .ThenInclude(d => d.Item)
            .Where(x => !x.IsDelete)
            .Select(x => new DOResponseDto
            {
                DoId = x.DoId,
                DoNumber = x.DoNumber,
                ScannerType = x.ScannerType,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                Details = x.Details.Select(d => new DODetailResponseDto
                {
                    DoDetailId = d.DoDetailId,
                    ItemId = d.ItemId,
                    ItemName = d.Item.Name,
                    QtyRequired = d.QtyRequired
                }).ToList()
            })
            .ToListAsync();

            DailyFileLogger.Info($"Successfully retrieved DO data, total: {result.Count}");
            return result;
        }
        catch(Exception ex)
        {
            DailyFileLogger.Error("Failed to retrieve DO data", ex);
            throw;
        }
    }

    public async Task<DOResponseDto?> GetByIdAsync(string id)
    {
        var result = await _db.DOs
            .Include(x => x.Details)
            .ThenInclude(d => d.Item)   
            .FirstOrDefaultAsync(x => x.DoId == id && !x.IsDelete);

        if (result == null) return null;

        return new DOResponseDto
        {
            DoId = result.DoId,
            DoNumber = result.DoNumber,
            ScannerType = result.ScannerType,
            Status = result.Status,
            CreatedAt = result.CreatedAt,
            Details = result.Details.Select(d => new DODetailResponseDto
            {
                DoDetailId = d.DoDetailId,
                ItemId = d.ItemId,
                ItemName = d.Item?.Name,
                QtyRequired = d.QtyRequired
            }).ToList()
        };
    }

    public async Task CreateAsync(PickingListDTO request, string createdBy)
    {
        try
        {
            var doEntity = new DO
            {
                DoId = Guid.NewGuid().ToString(),
                DoNumber = request.DoNumber,
                Status = "DRAFT",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            var details = request.Details.Select(d => new DODetail
            {
                DoDetailId = Guid.NewGuid().ToString(),
                DoId = doEntity.DoId,
                ItemId = d.ItemId,
                QtyRequired = d.QtyRequired
            }).ToList();

            _db.DOs.Add(doEntity);
            _db.DODetails.AddRange(details);

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"Successfully created new DO with ID: {doEntity.DoId}");
        } catch(Exception ex)
        {
            DailyFileLogger.Error("Failed to create new DO", ex);
            throw;
        }
        
    }
    public async Task UpdateAsync(string id, PickingListUpdateDTO dto)
    {
        try
        {
            var doEntity = await _db.DOs
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.DoId == id && !x.IsDelete);

            if (doEntity == null)
            {
                DailyFileLogger.Warn($"DO with ID: {id} not found");
                throw new Exception("DO not found");
            }

            if (doEntity.Status != "DRAFT")
            {
                DailyFileLogger.Warn($"DO with ID: {id} cannot be updated because status is not DRAFT");
                throw new Exception("DO can only be updated if the status is still DRAFT");
            }

            doEntity.DoNumber = dto.DoNumber;
            doEntity.ScannerType = dto.ScannerType;

            _db.DODetails.RemoveRange(doEntity.Details);

            var newDetails = dto.Details.Select(d => new DODetail
            {
                DoDetailId = Guid.NewGuid().ToString(),
                DoId = doEntity.DoId,
                ItemId = d.ItemId,
                QtyRequired = d.QtyRequired
            }).ToList();

            _db.DODetails.AddRange(newDetails);

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"Successfully updated DO with ID: {id}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Failed to update DO with ID: {id}", ex);
            throw;
        }
    }

    public async Task UpdateStatusAsync(string id, string status)
    {
        try
        {
            var dO = await _db.DOs.FindAsync(id);

            if (dO == null || dO.IsDelete == true)
            {
                DailyFileLogger.Warn($"DO with ID: {id} not found for status update");
                throw new Exception("DO not found");
            }

            dO.Status = status;

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"Successfully updated DO status for ID: {id} to {status}");
        } catch(Exception ex)
        {
            DailyFileLogger.Error($"Failed to update DO status for ID: {id}", ex);
            throw;
        }

    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var doData = await _db.DOs.FindAsync(id);

            if (doData == null || doData.IsDelete)
                throw new Exception("DO not found");
            doData.IsDelete = true;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"DO with ID {id} successfully deleted");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Failed to delete DO with ID {id}", ex);
            throw;
        }
    }


}