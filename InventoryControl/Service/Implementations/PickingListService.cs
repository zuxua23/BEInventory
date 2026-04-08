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

            DailyFileLogger.Info($"Berhasil mengambil data DO, total: {result.Count}");
            return result;
        }
        catch(Exception ex)
        {
            DailyFileLogger.Error("Gagal mengambil data DO", ex);
            throw;
        }
    }

    public async Task<DO?> GetByIdAsync(string id)
    {
        try
        {
            return await _db.DOs
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.DoId == id && !x.IsDelete);

        }
        catch(Exception ex)
        {
            DailyFileLogger.Error($"Gagal mengambil data DO dengan ID: {id}", ex);
            throw;
        }

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
            DailyFileLogger.Info($"Berhasil membuat DO baru dengan ID: {doEntity.DoId}");

        } catch(Exception ex)
        {
            DailyFileLogger.Error("Gagal membuat DO baru", ex);
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
                DailyFileLogger.Warn($"DO dengan ID: {id} tidak ditemukan");
                throw new Exception("DO tidak ditemukan");
            }

            if (doEntity.Status != "DRAFT")
            {
                DailyFileLogger.Warn($"DO dengan ID: {id} tidak bisa diupdate karena status bukan DRAFT");
                throw new Exception("DO hanya bisa diupdate jika status masih DRAFT");
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

            DailyFileLogger.Info($"Berhasil update DO dengan ID: {id}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Gagal update DO dengan ID: {id}", ex);
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
                    DailyFileLogger.Warn($"DO dengan ID: {id} tidak ditemukan untuk pembaruan status");
                throw new Exception("DO tidak ditemukan");
            }

            dO.Status = status;

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"Berhasil memperbarui status DO dengan ID: {id} menjadi {status}");
        } catch(Exception ex)
        {
            DailyFileLogger.Error($"Gagal memperbarui status DO dengan ID: {id}", ex);
            throw;
        }

    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var doData = await _db.DOs.FindAsync(id);

            if (doData == null || doData.IsDelete)
                throw new Exception("DO tidak ditemukan");

            doData.IsDelete = true;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"DO dengan ID {id} berhasil dihapus");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Gagal menghapus DO dengan ID {id}", ex);
            throw;
        }
    }
}