using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

public class ReaderScan
{
    private readonly AppDBContext _db;
    private readonly IStockOutService _stockOutService;
    private readonly ImpinjHelper _reader;

    private static ConcurrentDictionary<string, bool> scannedTags = new();

    public ReaderScan(AppDBContext db, IStockOutService stockOutService)
    {
        _db = db;
        _stockOutService = stockOutService;
        _reader = new ImpinjHelper();
    }
    private static HashSet<string> allowedTags = new();

    public async Task StartScanAsync(StockOutResponseDto dto, string user)
    {
        var reader = await _db.Readers
            .FirstOrDefaultAsync(r => r.RdrId == dto.ReaderId && !r.IsDelete);

        if (reader == null)
        {
            DailyFileLogger.Warn($"Reader dengan RdrId {dto.ReaderId} tidak ditemukan.");
            throw new Exception("Reader tidak ditemukan");
        }


        allowedTags = (await _db.TransactionDetails
            .Include(x => x.Transaction)
            .Include(x => x.Tag)
            .Where(x =>
                x.Transaction.TrsType == "STOCK_PREPARATION" &&
                x.Transaction.ReferenceId == dto.DoId)
            .Select(x => x.Tag.EpcTag)
            .ToListAsync())
            .ToHashSet();

        _reader.Connect(reader.IpAddress);

        scannedTags.Clear();

        _reader.StartReading(async epc =>
        {
            if (!scannedTags.TryAdd(epc, true))
                return;

            await _stockOutService.ScanStockOutAsync(dto, user);
        });
    }

    public void StopScan()
    {
        _reader.Stop();
        scannedTags.Clear();
    }
}