using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

namespace InventoryControl.Service.Implementations;

public class ReaderService : IReaderService
{
    private readonly AppDBContext _db;

    public ReaderService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<List<ReaderResponseDto>> GetAllAsync()
    {
        try
        {
             var reader = await _db.Readers
                .Include(r => r.LocationNavigation)
                .Where(r => !r.IsDelete)
                .Select(r => new ReaderResponseDto
                {
                    Id = r.Id,
                    RdrId = r.RdrId,
                    RdrName = r.Name,
                    LocId = r.LocationId,
                    LocationName = r.LocationNavigation.Name,
                    IpAddress = r.IpAddress
                })
                .ToListAsync();
            return reader;
        } catch(Exception ex)
        {
            DailyFileLogger.Error("Error di GetAllAsync.", ex);
            throw;
        }
       
    }
    public async Task<ReaderResponseDto> GetByIdAsync(string id)
    {
        try
        {
            var reader = await _db.Readers
                .Include(r => r.LocationNavigation)
                .Where(x => x.Id == id && x.IsDelete == false)
                .Select(x => new ReaderResponseDto
                {
                    Id = x.Id,
                    RdrId = x.RdrId,
                    RdrName = x.Name,
                    LocId = x.LocationId,
                    LocationName = x.LocationNavigation.Name,
                    IpAddress = x.IpAddress
                })
                .FirstOrDefaultAsync();

            if (reader != null)
                DailyFileLogger.Info($"GetByIdAsync berhasil untuk ID {id}.");
            else
                DailyFileLogger.Warn($"GetByIdAsync: Reader dengan ID {id} tidak ditemukan.");

            return reader;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error($"Error di GetByIdAsync untuk ID {id}.", ex);
            throw;
        }
    }
    public async Task CreateAsync(ReaderDto dto, string createdBy)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.RdrId))
                DailyFileLogger.Warn("Reader ID tidak boleh kosong");

            if (string.IsNullOrWhiteSpace(dto.IpAddress))
                DailyFileLogger.Warn("IP Address tidak boleh kosong");

            var existingReader = await _db.Readers
                .FirstOrDefaultAsync(x => x.RdrId == dto.RdrId && !x.IsDelete);

            if (existingReader != null)
            {
                DailyFileLogger.Warn($"CreateAsync: Reader ID {dto.RdrId} sudah digunakan oleh Reader dengan ID {existingReader.Id}");
                    throw new Exception("Reader ID sudah digunakan");
            }

            var location = await _db.Locations
                .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
            {
                DailyFileLogger.Warn($"CreateAsync: Location dengan ID {dto.LocId} tidak ditemukan");
                throw new Exception("Location tidak ditemukan");
            }

            var reader = new Reader
            {
                Id = Guid.NewGuid().ToString(),
                RdrId = dto.RdrId,
                LocationId = location.Id,
                Name = dto.RdrName,
                IpAddress = dto.IpAddress,
                Status = "READY",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Readers.Add(reader);
            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"CreateAsync berhasil untuk Reader ID {dto.RdrId}.");
        }
        catch(Exception ex)
        {
            DailyFileLogger.Error($"Error di CreateAsync untuk Reader ID {dto.RdrId}.", ex);
            throw;

        }
        
    }
    public async Task UpdateAsync(string id, ReaderDto dto, string updatedBy)
    {
        try
        {
            var reader = await _db.Readers
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDelete);

            if (reader == null)
            {
                DailyFileLogger.Warn($"UpdateAsync: Reader dengan ID {id} tidak ditemukan");
                throw new Exception("Reader tidak ditemukan");
            }

            if (string.IsNullOrWhiteSpace(dto.RdrId))
                DailyFileLogger.Warn("Reader ID tidak boleh kosong");

            if (string.IsNullOrWhiteSpace(dto.IpAddress))
                DailyFileLogger.Warn("IP Address tidak boleh kosong");

            var location = await _db.Locations
                .FirstOrDefaultAsync(x => x.Id == dto.LocId);

            if (location == null)
            {
                DailyFileLogger.Warn($"UpdateAsync: Location dengan ID {dto.LocId} tidak ditemukan");
                throw new Exception("Location tidak ditemukan");
            }

            var duplicateReader = await _db.Readers
                .FirstOrDefaultAsync(x => x.RdrId == dto.RdrId && x.Id != id && !x.IsDelete);

            if (duplicateReader != null)
            {
                DailyFileLogger.Warn($"UpdateAsync: Reader ID {dto.RdrId} sudah digunakan oleh Reader dengan ID {duplicateReader.Id}");
                throw new Exception("Reader ID sudah digunakan");
            }

            reader.RdrId = dto.RdrId;
            reader.Name = dto.RdrName;
            reader.LocationId = location.Id;
            reader.IpAddress = dto.IpAddress;
            reader.UpdatedBy = updatedBy;
            reader.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"UpdateAsync berhasil untuk ID {id}.");
        } catch(Exception ex)
        {
            DailyFileLogger.Error($"Error di UpdateAsync untuk ID {id}.", ex);
            throw;
        }
        
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var reader = await _db.Readers
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDelete);

            if (reader == null)
                DailyFileLogger.Warn("Reader tidak ditemukan");

            reader.IsDelete = true;
            reader.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"DeleteAsync berhasil untuk ID {id}.");
        } catch(Exception ex)
        {
            DailyFileLogger.Error($"Error di DeleteAsync untuk ID {id}.", ex);
            throw;
        }
      
    }
}