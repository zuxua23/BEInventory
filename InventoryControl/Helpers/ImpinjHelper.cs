using Impinj.OctaneSdk;
using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using System.Collections.Concurrent;
using static InventoryControl.Service.Implementations.StockOutService;

public class ImpinjReaderService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<string, ImpinjReader> _readers = new();
    private readonly ConcurrentDictionary<string, DateTime> _tagCache = new();

    public ImpinjReaderService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void StartReader(string readerId, string ip)
    {
        if (_readers.ContainsKey(readerId))
            return;

        var reader = new ImpinjReader();
        reader.Connect(ip);

        var settings = reader.QueryDefaultSettings();
        settings.Report.Mode = ReportMode.Individual;
        settings.Report.IncludeAntennaPortNumber = true;

        reader.ApplySettings(settings);

        reader.TagsReported += async (s, report) =>
        {
            await HandleTags(readerId, report);
        };

        reader.Start();

        _readers.Add(readerId, reader);
    }

    public void StopReader(string readerId)
    {
        if (!_readers.ContainsKey(readerId)) return;

        _readers[readerId].Stop();
        _readers[readerId].Disconnect();

        _readers.Remove(readerId);
        RfidSession.Remove(readerId);
    }

    private async Task HandleTags(string readerId, TagReport report)
    {
        var doId = RfidSession.Get(readerId);
        if (doId == null) return;

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IStockOutService>();

        foreach (var tag in report.Tags)
        {
            var epc = tag.Epc.ToString();

            if (tag.PeakRssiInDbm < -60) continue;

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
}