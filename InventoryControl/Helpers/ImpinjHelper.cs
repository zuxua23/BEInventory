using Impinj.OctaneSdk;
using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using System.Collections.Concurrent;

using static InventoryControl.Service.Implementations.StockOutService;

public class ImpinjReaderService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ConcurrentDictionary<string, ImpinjReader>
        _readers = new();

    private readonly ConcurrentDictionary<string, DateTime>
        _tagCache = new();

    private const double RSSI_THRESHOLD = -90;

    private const int CACHE_TTL_SECONDS = 10;

    public ImpinjReaderService(
        IServiceScopeFactory scopeFactory
    )
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartReader(
        string readerId,
        string ip
    )
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

            var reader = new ImpinjReader();

            int retry = 3;

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

                    await Task.Delay(1000);
                }
            }

            var settings =
                reader.QueryDefaultSettings();

            settings.Antennas.GetAntenna(1).IsEnabled = true;
            settings.Antennas.GetAntenna(1).TxPowerInDbm = 30;

            settings.Report.Mode =
                ReportMode.Individual;

            settings.Report
                .IncludeAntennaPortNumber =
                    true;

            settings.Report
                .IncludeFirstSeenTime =
                    true;

            reader.ApplySettings(settings);

            SystemLogger.Info(
                $"RFID reader settings applied successfully. ReaderId='{readerId}'."
            );

            reader.TagsReported +=
                HandleTagsWrapper;

            reader.Start();

            _readers.TryAdd(
                readerId,
                reader
            );

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

            if (
                !_readers.TryGetValue(
                    readerId,
                    out var reader
                )
            )
            {
                SystemLogger.Warn(
                    $"RFID reader '{readerId}' was not found."
                );

                return;
            }

            reader.Stop();

            reader.TagsReported -=
                HandleTagsWrapper;

            reader.Disconnect();

            SystemLogger.Info(
                $"RFID reader stopped successfully. ReaderId='{readerId}'."
            );

            _readers.TryRemove(
                readerId,
                out _
            );

            RfidSession.Remove(readerId);

            SystemLogger.Info(
                $"RFID session removed successfully. ReaderId='{readerId}'."
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

    private async void HandleTagsWrapper(
        object sender,
        TagReport report
    )
    {
        try
        {
            var reader =
                sender as ImpinjReader;

            var readerId = _readers
                .FirstOrDefault(x =>
                    x.Value == reader
                )
                .Key;

            if (readerId == null)
            {
                SystemLogger.Warn(
                    "RFID tag event received without valid reader session."
                );

                return;
            }

            await HandleTags(
                readerId,
                report
            );
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "An error occurred while processing RFID tag wrapper event.",
                ex
            );
        }
    }

    private async Task HandleTags(
        string readerId,
        TagReport report
    )
    {
        try
        {
            var doId =
                RfidSession.Get(readerId);

            if (doId == null)
            {
                SystemLogger.Warn(
                    $"RFID session not found for ReaderId '{readerId}'."
                );

                return;
            }

            using var scope =
                _scopeFactory.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IStockOutService>();

            CleanupCache();

            int processedCount = 0;
            int skippedCount = 0;

            foreach (var tag in report.Tags)
            {
                var epc = tag.Epc
                    .ToString()
                    .Replace(" ", "")
                    .Trim()
                    .ToUpper();

                if (tag.PeakRssiInDbm < RSSI_THRESHOLD)
                {
                    skippedCount++;
                    continue;
                }

                if (
                    _tagCache.ContainsKey(epc) &&
                    (DateTime.UtcNow - _tagCache[epc]).TotalSeconds < 2
                )
                {
                    skippedCount++;
                    continue;
                }

                _tagCache[epc] = DateTime.UtcNow;

                processedCount++;

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
                if (
                    (
                        DateTime.UtcNow -
                        _tagCache[key]
                    ).TotalSeconds >
                    CACHE_TTL_SECONDS
                )
                {
                    if (
                        _tagCache.TryRemove(
                            key,
                            out _
                        )
                    )
                    {
                        removedCount++;
                    }
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