using Impinj.OctaneSdk;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Models;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

using static InventoryControl.Service.Implementations.StockOutService;

public class ImpinjReaderService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RfidOptionsModel _rfidOptions;

    private readonly ConcurrentDictionary<string, ImpinjReader> _readers = new();
    private readonly ConcurrentDictionary<string, DateTime> _tagCache = new();
    private readonly ConcurrentDictionary<string, int> _duplicateScanIntervals = new();


    public ImpinjReaderService(
        IServiceScopeFactory scopeFactory,
        IOptions<RfidOptionsModel> rfidOptions
    )
    {
        _scopeFactory = scopeFactory;
        _rfidOptions = rfidOptions.Value;
    }

    public async Task StartReader(string readerId, string ip)
    {
        try
        {
            SystemLogger.Info(
                $"Starting RFID reader connection. ReaderId='{readerId}', IP='{ip}'."
            );

            if (_readers.ContainsKey(readerId))
            {
                SystemLogger.Warn(
                    $"RFID reader '{readerId}' is already running."
                );

                return;
            }
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();

            var readerData = await db.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == readerId &&
                    !x.IsDelete
                );

            if (readerData == null)
                throw new Exception("Reader not found.");

            var antennaSettings = await db.ReaderSettings
                .AsNoTracking()
                .Where(x =>
                    x.ReaderId == readerId &&
                    !x.IsDelete
                )
                .OrderBy(x => x.AntennaNo)
                .ToListAsync();

            if (!antennaSettings.Any())
                throw new Exception("Reader antenna setting not found.");

            if (!antennaSettings.Any(x => x.IsEnabled))
                throw new Exception("At least one antenna must be enabled.");

            var reader = new ImpinjReader();

            int retry = _rfidOptions.ConnectionRetry;

            while (retry-- > 0)
            {
                try
                {
                    SystemLogger.Info(
                        $"Attempting RFID reader connection. ReaderId='{readerId}', RemainingRetry='{retry}'."
                    );

                    reader.Connect(ip);

                    SystemLogger.Info(
                        $"RFID reader connected successfully. ReaderId='{readerId}'."
                    );

                    break;
                }
                catch (Exception ex)
                {
                    SystemLogger.Warn(
                        $"RFID reader connection failed. ReaderId='{readerId}', RemainingRetry='{retry}'."
                    );

                    if (retry == 0)
                    {
                        SystemLogger.Error(
                            $"Failed to connect RFID reader '{readerId}'.",
                            ex
                        );

                        throw;
                    }

                    await Task.Delay(_rfidOptions.RetryDelayMs);
                }
            }

            var settings = reader.QueryDefaultSettings();

            foreach (var ant in antennaSettings)
            {
                var antenna = settings.Antennas.GetAntenna(
                    (ushort)ant.AntennaNo
                );

                antenna.IsEnabled = ant.IsEnabled;

                if (!ant.IsEnabled)
                    continue;

                antenna.TxPowerInDbm = ant.TxPower;
                antenna.RxSensitivityInDbm = ant.Sensitivity;
            }

            settings.SearchMode = readerData.SearchMode switch
            {
                ReaderSearchMode.SingleTarget => SearchMode.SingleTarget,
                ReaderSearchMode.DualTarget => SearchMode.DualTarget,
                _ => SearchMode.DualTarget
            };

            settings.Session = readerData.Session switch
            {
                ReaderSession.S0 => 0,
                ReaderSession.S1 => 1,
                ReaderSession.S2 => 2,
                ReaderSession.S3 => 3,
                _ => 2
            };

            settings.Report.Mode = ReportMode.Individual;
            settings.Report.IncludeAntennaPortNumber = true;
            settings.Report.IncludeFirstSeenTime = true;

            _duplicateScanIntervals[readerId] =
                readerData.DuplicateScanInterval <= 0
                    ? 2
                    : readerData.DuplicateScanInterval;

            reader.ApplySettings(settings);

            SystemLogger.Info(
                $"RFID reader settings applied successfully. ReaderId='{readerId}'."
            );

            reader.TagsReported += HandleTagsWrapper;

            reader.Start();

            _readers.TryAdd(readerId, reader);

            SystemLogger.Info(
                $"RFID reader started successfully. ReaderId='{readerId}'."
            );
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                $"An error occurred while starting RFID reader '{readerId}'.",
                ex
            );

            throw;
        }
    }

    public void StopReader(string readerId)
    {
        try
        {
            SystemLogger.Info(
                $"Stopping RFID reader '{readerId}'."
            );

            if (!_readers.TryGetValue(readerId, out var reader))
            {
                SystemLogger.Warn(
                    $"RFID reader '{readerId}' was not found."
                );

                return;
            }

            reader.Stop();
            reader.TagsReported -= HandleTagsWrapper;
            reader.Disconnect();

            _readers.TryRemove(readerId, out _);
            _duplicateScanIntervals.TryRemove(readerId, out _);

            RfidSession.Remove(readerId);

            SystemLogger.Info(
                $"RFID reader stopped successfully. ReaderId='{readerId}'."
            );
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                $"An error occurred while stopping RFID reader '{readerId}'.",
                ex
            );
        }
    }

    private async void HandleTagsWrapper(object sender, TagReport report)
    {
        try
        {
            var reader = sender as ImpinjReader;

            var readerId = _readers
                .FirstOrDefault(x => x.Value == reader)
                .Key;

            if (string.IsNullOrWhiteSpace(readerId))
            {
                SystemLogger.Warn(
                    "RFID tag event received without valid reader session."
                );

                return;
            }

            await HandleTags(readerId, report);
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "An error occurred while processing RFID tag wrapper event.",
                ex
            );
        }
    }

    private async Task HandleTags(string readerId, TagReport report)
    {
        try
        {
            var doId = RfidSession.Get(readerId);

            if (string.IsNullOrWhiteSpace(doId))
            {
                SystemLogger.Warn(
                    $"RFID session not found for ReaderId '{readerId}'."
                );

                return;
            }

            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IStockOutService>();

            CleanupCache();

            foreach (var tag in report.Tags)
            {
                var epc = tag.Epc
                    .ToString()
                    .Replace(" ", "")
                    .Trim()
                    .ToUpper();

                var cacheKey = $"{readerId}:{doId}:{epc}";

                var duplicateInterval =
                    _duplicateScanIntervals.TryGetValue(readerId, out var interval)
                        ? interval
                        : 2;

                if (
                    _tagCache.TryGetValue(cacheKey, out var lastScanTime) &&
                    (DateTime.UtcNow - lastScanTime).TotalSeconds < duplicateInterval
                )
                {
                    continue;
                }

                _tagCache[cacheKey] = DateTime.UtcNow;

                await service.ScanStockOutAsync(
                    new StockOutResponseDto
                    {
                        Epc = epc,
                        ReaderId = readerId,
                        DoId = doId
                    },
                    "RFID_SYSTEM"
                );
            }
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                $"An error occurred while processing RFID tags for ReaderId '{readerId}'.",
                ex
            );
        }
    }

    private void CleanupCache()
    {
        try
        {
            int removedCount = 0;

            foreach (var key in _tagCache.Keys)
            {
                if (!_tagCache.TryGetValue(key, out var scannedAt))
                    continue;

                if ((DateTime.UtcNow - scannedAt).TotalSeconds <= _rfidOptions.CacheTTLSeconds)
                    continue;

                if (_tagCache.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                SystemLogger.Info(
                    $"RFID cache cleanup completed. Removed='{removedCount}'."
                );
            }
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "An error occurred during RFID cache cleanup.",
                ex
            );
        }
    }
}