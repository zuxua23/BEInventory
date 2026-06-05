namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public class ReaderService : IReaderService
{
    private readonly AppDBContext _db;

    public ReaderService( AppDBContext db )
    {
        _db = db;
    }

    public async Task<List<ReaderResponseDto>> GetAllAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving all active readers."
            );

            var readers = await _db.Readers
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

            DailyFileLogger.Info(
                $"Successfully retrieved {readers.Count} reader(s)."
            );

            return readers;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving readers.",
                ex
            );

            throw;
        }
    }

    public async Task<ReaderResponseDto?> GetByIdAsync(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving reader with ID '{id}'."
            );

            var reader = await _db.Readers
                .Include(r => r.LocationNavigation)
                .Where(x =>
                    x.Id == id &&
                    !x.IsDelete
                )
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

            if (reader == null)
            {
                DailyFileLogger.Warn(
                    $"Reader with ID '{id}' was not found."
                );

                return null;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved reader with ID '{id}'."
            );

            return reader;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving reader with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task CreateAsync(ReaderDto dto, string createdBy)
    {
        try
        {
            DailyFileLogger.Info(
                $"Creating new reader with ReaderId '{dto.RdrId}'.",
                createdBy
            );

            if (string.IsNullOrWhiteSpace(dto.RdrId))
            {
                DailyFileLogger.Warn(
                    "Reader ID cannot be empty.",
                    createdBy
                );

                throw new Exception(
                    "Reader ID cannot be empty."
                );
            }

            if (string.IsNullOrWhiteSpace(dto.IpAddress))
            {
                DailyFileLogger.Warn(
                    "IP Address cannot be empty.",
                    createdBy
                );

                throw new Exception(
                    "IP Address cannot be empty."
                );
            }

            var existingReader = await _db.Readers
                .FirstOrDefaultAsync(x =>
                    x.RdrId == dto.RdrId &&
                    !x.IsDelete
                );

            if (existingReader != null)
            {
                DailyFileLogger.Warn(
                    $"Reader ID '{dto.RdrId}' is already in use.",
                    createdBy
                );

                throw new Exception(
                    "Reader ID already exists."
                );
            }

            var location = await _db.Locations
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.LocId &&
                    !x.IsDelete
                );

            if (location == null)
            {
                DailyFileLogger.Warn(
                    $"Location with ID '{dto.LocId}' was not found.",
                    createdBy
                );

                throw new Exception(
                    "Location not found."
                );
            }

            var reader = new Reader
            {
                Id = Guid.NewGuid().ToString(),
                RdrId = dto.RdrId,
                LocationId = location.Id,
                Name = dto.RdrName,
                IpAddress = dto.IpAddress,
                Status = "READY",
                SearchMode = ReaderSearchMode.DualTarget,
                Session = ReaderSession.S2,
                DuplicateScanInterval = 10,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsDelete = false
            };

            _db.Readers.Add(reader);

            for (int i = 1; i <= 4; i++)
            {
                _db.ReaderSettings.Add(new ReaderSettings
                {
                    Id = Guid.NewGuid().ToString(),
                    ReaderId = reader.Id,
                    AntennaNo = i,
                    IsEnabled = i == 1,
                    TxPower = 20,
                    Sensitivity = -70,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    IsDelete = false
                });
            }

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Reader successfully created with ReaderId '{dto.RdrId}'.",
                createdBy
            );

            DailyFileLogger.Audit(
                action: "CREATE",
                entity: "READER",
                entityId: reader.RdrId,
                performedBy: createdBy,
                description:
                    $"Created reader '{dto.RdrName}' with IP '{dto.IpAddress}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while creating reader '{dto.RdrId}'.",
                ex,
                createdBy
            );

            throw;
        }
    }

    public async Task UpdateAsync(string id, ReaderDto dto, string updatedBy)
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating reader with ID '{id}'.",
                updatedBy
            );

            var reader = await _db.Readers
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    !r.IsDelete
                );

            if (reader == null)
            {
                DailyFileLogger.Warn(
                    $"Reader with ID '{id}' was not found.",
                    updatedBy
                );

                throw new Exception(
                    "Reader not found."
                );
            }

            if (string.IsNullOrWhiteSpace(dto.RdrId))
            {
                DailyFileLogger.Warn(
                    "Reader ID cannot be empty.",
                    updatedBy
                );

                throw new Exception(
                    "Reader ID cannot be empty."
                );
            }

            if (string.IsNullOrWhiteSpace(dto.IpAddress))
            {
                DailyFileLogger.Warn(
                    "IP Address cannot be empty.",
                    updatedBy
                );

                throw new Exception(
                    "IP Address cannot be empty."
                );
            }

            var location = await _db.Locations
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.LocId &&
                    !x.IsDelete
                );

            if (location == null)
            {
                DailyFileLogger.Warn(
                    $"Location with ID '{dto.LocId}' was not found.",
                    updatedBy
                );

                throw new Exception(
                    "Location not found."
                );
            }

            var duplicateReader = await _db.Readers
                .FirstOrDefaultAsync(x =>
                    x.RdrId == dto.RdrId &&
                    x.Id != id &&
                    !x.IsDelete
                );

            if (duplicateReader != null)
            {
                DailyFileLogger.Warn(
                    $"Reader ID '{dto.RdrId}' is already in use.",
                    updatedBy
                );

                throw new Exception(
                    "Reader ID already exists."
                );
            }

            var oldReaderId = reader.RdrId;
            var oldName = reader.Name;
            var oldIpAddress = reader.IpAddress;
            var oldLocationName = reader.LocationNavigation.Name;

            reader.RdrId = dto.RdrId;
            reader.Name = dto.RdrName;
            reader.LocationId = location.Id;
            reader.IpAddress = dto.IpAddress;
            reader.UpdatedBy = updatedBy;
            reader.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Reader successfully updated. ID='{id}'.",
                updatedBy
            );

            DailyFileLogger.Audit(
                action: "UPDATE",
                entity: "READER",
                entityId: reader.RdrId,
                performedBy: updatedBy,
                description:
                    $"Updated reader from ReaderId='{oldReaderId}', Name='{oldName}', IP='{oldIpAddress}', Location='{oldLocationName}' " +
                    $"to ReaderId='{dto.RdrId}', Name='{dto.RdrName}', IP='{dto.IpAddress}', Location='{location.Name}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while updating reader with ID '{id}'.",
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
                $"Deleting reader with ID '{id}'.",
                deletedBy
            );

            var reader = await _db.Readers
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    !r.IsDelete
                );

            if (reader == null)
            {
                DailyFileLogger.Warn(
                    $"Reader with ID '{id}' was not found."
                );

                throw new Exception(
                    "Reader not found."
                );
            }

            reader.IsDelete = true;
            reader.UpdatedAt = DateTime.UtcNow;
            reader.UpdatedBy = deletedBy;

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Reader successfully soft deleted. ID='{id}'."
            );

            DailyFileLogger.Audit(
                action: "DELETE",
                entity: "READER",
                entityId: reader.RdrId,
                performedBy: deletedBy,
                description:
                    $"Soft deleted reader '{reader.Name}'."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while deleting reader with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task<ReaderSettingDto> GetReaderSettingAsync(string readerId)
    {
        try
        {
            var reader = await _db.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == readerId &&
                    !x.IsDelete
                );

            if (reader == null)
                throw new Exception("Reader not found.");

            var antennas = await _db.ReaderSettings
                .AsNoTracking()
                .Where(x =>
                    x.ReaderId == readerId &&
                    !x.IsDelete
                )
                .OrderBy(x => x.AntennaNo)
                .Select(x => new ReaderSettingItemDto
                {
                    AntennaNo = x.AntennaNo,
                    IsEnabled = x.IsEnabled,
                    TxPower = x.TxPower,
                    Sensitivity = x.Sensitivity
                })
                .ToListAsync();

            return new ReaderSettingDto
            {
                ReaderId = reader.Id,
                SearchMode = reader.SearchMode,
                Session = reader.Session,
                DuplicateScanInterval = reader.DuplicateScanInterval <= 0
                    ? 2
                    : reader.DuplicateScanInterval,
                Antennas = antennas
            };
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"Failed to retrieve reader setting. ReaderId='{readerId}'.",
                ex
            );

            throw;
        }
    }

    public async Task SaveReaderSettingAsync(ReaderSettingDto dto,string user)
    {
        try
        {
            DailyFileLogger.Info(
                $"Updating reader setting. ReaderId='{dto.ReaderId}'.",
                user
            );

            if (dto == null)
                throw new Exception("Reader setting data is required.");

            if (string.IsNullOrWhiteSpace(dto.ReaderId))
                throw new Exception("Reader is required.");

            if (dto.Antennas == null || !dto.Antennas.Any())
                throw new Exception("Antenna setting is required.");

            if (!dto.Antennas.Any(x => x.IsEnabled))
                throw new Exception("At least one antenna must be enabled.");

            var reader = await _db.Readers
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.ReaderId &&
                    !x.IsDelete
                );

            if (reader == null)
                throw new Exception("Reader not found.");


            reader.SearchMode = dto.SearchMode;
            reader.Session = dto.Session;
            reader.DuplicateScanInterval =
                dto.DuplicateScanInterval <= 0
                    ? 2
                    : dto.DuplicateScanInterval;

            reader.UpdatedBy = user;
            reader.UpdatedAt = DateTime.UtcNow;

            var settings =
                await _db.ReaderSettings
                    .Where(x =>
                        x.ReaderId == dto.ReaderId &&
                        !x.IsDelete
                    )
                    .ToListAsync();

            foreach (var antenna in dto.Antennas)
            {
                var setting =
                    settings.FirstOrDefault(x =>
                        x.AntennaNo ==
                        antenna.AntennaNo
                    );

                if (setting == null)
                {
                    setting =
                        new ReaderSettings
                        {
                            Id = Guid.NewGuid().ToString(),
                            ReaderId = dto.ReaderId,
                            AntennaNo = antenna.AntennaNo,
                            CreatedBy = user,
                            CreatedAt = DateTime.UtcNow
                        };

                    _db.ReaderSettings.Add(
                        setting
                    );
                }


                setting.IsEnabled = antenna.IsEnabled;
                setting.TxPower = antenna.TxPower;
                setting.Sensitivity = antenna.Sensitivity;
                setting.UpdatedBy = user;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            DailyFileLogger.Audit(
                action: "UPDATE_READER_SETTING",
                entity: "READER",
                entityId: dto.ReaderId,
                performedBy: user,
                description:
                    $"Updated antenna settings."
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"Failed to update reader setting. ReaderId='{dto.ReaderId}'.",
                ex,
                user
            );

            throw;
        }
    }
}