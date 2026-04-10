using Impinj.OctaneSdk;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using System.Collections.Concurrent;
using static InventoryControl.Service.Implementations.StockOutService;

public class ImpinjReaderService
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Thread-safe dictionary
    private readonly ConcurrentDictionary<string, ImpinjReader> _readers = new();

    // Cache EPC untuk anti double scan
    private readonly ConcurrentDictionary<string, DateTime> _tagCache = new();

    private const double RSSI_THRESHOLD = -60;
    private const int CACHE_TTL_SECONDS = 10;

    public ImpinjReaderService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }



    public async Task StartReader(string readerId, string ip)
    {
        if (_readers.ContainsKey(readerId))
            return;

        var reader = new ImpinjReader();

        // Retry connect
        int retry = 3;
        while (retry-- > 0)
        {
            try
            {
                reader.Connect(ip);
                break;
            }
            catch
            {
                if (retry == 0) throw;
                await Task.Delay(1000);
            }
        }

        var settings = reader.QueryDefaultSettings();

        settings.Report.Mode = ReportMode.Individual;
        settings.Report.IncludeAntennaPortNumber = true;
        settings.Report.IncludeFirstSeenTime = true;

        settings.ReaderMode = ReaderMode.AutoSetDenseReader;

        reader.ApplySettings(settings);

        // Register event handler (NO anonymous function!)
        reader.TagsReported += HandleTagsWrapper;

        reader.Start();

        // Atomic add
        _readers.TryAdd(readerId, reader);

        Console.WriteLine($"Reader {readerId} connected ({ip})");
    }



    public void StopReader(string readerId)
    {
        if (!_readers.TryGetValue(readerId, out var reader))
            return;

        try
        {
            reader.Stop();

            reader.TagsReported -= HandleTagsWrapper;

            reader.Disconnect();

            Console.WriteLine($"Reader {readerId} stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping reader {readerId}: {ex.Message}");
        }

        _readers.TryRemove(readerId, out _);

        // Remove session
        RfidSession.Remove(readerId);
    }


    private async void HandleTagsWrapper(object sender, TagReport report)
    {
        try
        {
            Console.WriteLine($"Tags reported: {report.Tags.Count} from reader event");
            var reader = sender as ImpinjReader;

            var readerId = _readers
                .FirstOrDefault(x => x.Value == reader).Key;

            if (readerId != null)
                await HandleTags(readerId, report);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error HandleTagsWrapper: " + ex.Message);
        }
    }


    private async Task HandleTags(string readerId, TagReport report)
    {
        var doId = RfidSession.Get(readerId);
        if (doId == null) return;
        Console.WriteLine($"SESSION DO: {doId}");

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockOutService>();

        CleanupCache();

        foreach (var tag in report.Tags)
        {
            var epc = tag.Epc.ToString().Replace(" ", "").Trim().ToUpper();
            Console.WriteLine($"Tag detected: {epc}, RSSI: {tag.PeakRssiInDbm} dBm");
            // Filter RSSI
            if (tag.PeakRssiInDbm < RSSI_THRESHOLD)
                continue;

            // Anti duplicate scan (within 2 seconds)
            if (_tagCache.ContainsKey(epc) &&
                (DateTime.UtcNow - _tagCache[epc]).TotalSeconds < 2)
                continue;

            _tagCache[epc] = DateTime.UtcNow;

            await service.ScanStockOutAsync(new StockOutResponseDto
            {
                Epc = epc,
                ReaderId = readerId,
                DoId = doId
            }, "RFID_SYSTEM");
        }
    }

 
    private void CleanupCache()
    {
        foreach (var key in _tagCache.Keys)
        {
            if ((DateTime.UtcNow - _tagCache[key]).TotalSeconds > CACHE_TTL_SECONDS)
            {
                _tagCache.TryRemove(key, out _);
            }
        }
    }
}