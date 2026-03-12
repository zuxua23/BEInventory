using InTheHand.Net.Bluetooth;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using System.Text;

namespace InventoryControl.Service.Implementations;

public class PrintTagRegisService : IPrintTagRegisService
{
    private readonly AppDBContext _db;

    public PrintTagRegisService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<string> PrintAsync(PrintTagDto dto, string user)
    {
        try
        {
            if (dto.Qty <= 0)
            {
                DailyFileLogger.Warn("PrintAsync gagal: Qty <= 0");
                throw new Exception("Qty harus lebih dari 0");
            }

            var batchNo = $"PRN-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var item = await _db.Items.FirstOrDefaultAsync(x => x.Id == dto.ItemId);

            if (item == null)
            {
                DailyFileLogger.Warn($"PrintAsync gagal: Item {dto.ItemId} tidak ditemukan");
                throw new Exception("Item tidak ditemukan");
            }

            //var printerName = "SATO CL4NX Plus (SmaPri)";
            //var printerName = "SATO PRINTER_d";
            //var bdAddress = "84253f9fc39d";

            var lastTag = await _db.Tags
                .OrderByDescending(t => t.TagId)
                .FirstOrDefaultAsync();

            var lastNumber = await _db.Tags.CountAsync();

            if (lastTag != null)
                lastNumber = int.Parse(lastTag.TagId.Substring(3));

            for (int i = 0; i < dto.Qty; i++)
            {

                var tagId = $"TAG{lastNumber:D5}";

                var epc = $"A{item.ItmId}{lastNumber:D10}";

                var location = await _db.Locations
                .FirstOrDefaultAsync(x => x.LocId == "STAGING");

                if (location == null)
                {
                    DailyFileLogger.Warn("PrintAsync gagal: Location STAGING tidak ditemukan");
                    throw new Exception("Location STAGING tidak ditemukan");
                }

                var tag = new Tag
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tagId,
                    EpcTag = epc,
                    ItemId = dto.ItemId,
                    LocationId = location.Id,
                    Status = "PRINTED",
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Tags.Add(tag);
                await _db.SaveChangesAsync();


                _db.Histories.Add(new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = dto.ItemId,
                    Type = "PRINT",
                    Reference = batchNo,
                    Action = "CREATE",
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });

                // QR code data
                var qr = tagId;

                var sbpl = BuildSBPL(epc, item.Name, dto.Qty, qr);
                BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;
                //bool printed = PrinterHelper.SendStringToPrinter(printerName, sbpl);

                //var macAddress = "84253f9fc39d";
                //var macAddress = "84:25:3F:9F:C3:9D";

                //var bytes = SBPLStringToBytes(sbpl);
                //bool printed = BluetoothPrinterHelper.SendRawBytes(macAddress, bytes); Console.WriteLine(sbpl);
                //if (!printed)
                //{
                //    DailyFileLogger.Error($"PrintAsync gagal kirim ke printer. Tag: {tagId}", null);
                //    throw new Exception("Gagal mengirim data ke printer SATO");
                //}

                var test = "\u0002\u001BA\u001B%1\u001BH0100\u001BV0100\u001BP02TEST\u001BQ1\u001BZ\u0003";

                var bytes = Encoding.ASCII.GetBytes(test);

                BluetoothPrinterHelper.SendRawBytes("84253f9fc39d", bytes);
                Console.WriteLine("PRINT SBPL:");
                Console.WriteLine(sbpl);

                Console.WriteLine("BYTES LENGTH: " + bytes.Length);
                //Console.WriteLine("MAC: " + macAddress);
                DailyFileLogger.Info($"Tag berhasil dibuat dan dikirim ke printer. TagId: {tagId}");
            }

            await _db.SaveChangesAsync();
            DailyFileLogger.Info($"PrintAsync berhasil. BatchNo: {batchNo}");
            return batchNo;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di PrintAsync.", ex);
            throw;
        }
    }
    private byte[] SBPLStringToBytes(string sbpl)
    {
        var bytes = new List<byte>();

        foreach (char c in sbpl)
        {
            switch (c)
            {
                case '\u0002': bytes.Add(0x02); break;
                case '\u0003': bytes.Add(0x03); break;
                case '\u001B': bytes.Add(0x1B); break;
                default: bytes.Add((byte)c); break;
            }
        }

        return bytes.ToArray();
    }
    private string BuildSBPL(string epc, string itemName, int qty, string qr)
    {
        return $@"
AA3V+00000H+0000CS6#F5A1V00901H0300Z
APSWKlabel_ajg
IP0e:h,epc:{epc},fsw:1;
%1
H0040
V00866
2D30,L,06,1,0
DN0009,{qr}
%1
H0053
V00701
P02
RH0,SATO0.ttf,0,022,025,Item No : {itemName}
%1
H0096
V00699
P02
RH0,SATO0.ttf,0,022,025,Qty : {qty}
Q1
Z";
    }


    public async Task RegisterAsync(TagRegistrationDto dto, string user)
    {
        try
        {
            if (!dto.TagIds.Any())
            {
                DailyFileLogger.Warn("RegisterAsync gagal: TagIds kosong");
                throw new Exception("Tag tidak boleh kosong");
            }

            var tags = await _db.Tags
                .Where(t => dto.TagIds.Contains(t.TagId))
                .ToListAsync();

            if (!tags.Any())
            {
                DailyFileLogger.Warn("RegisterAsync gagal: Tag tidak ditemukan");
                throw new Exception("Tag tidak ditemukan");
            }

            var foundTagIds = tags.Select(t => t.TagId).ToHashSet();
            var missingTags = dto.TagIds.Where(id => !foundTagIds.Contains(id)).ToList();

            if (missingTags.Any())
            {
                DailyFileLogger.Warn($"RegisterAsync gagal: Tag tidak ditemukan {string.Join(",", missingTags)}");
                throw new Exception($"Tag tidak ditemukan: {string.Join(",", missingTags)}");
            }
            var reference = $"REG-{DateTime.UtcNow:yyyyMMddHHmmss}";


            foreach (var tag in tags)
            {
                if (tag.Status != "PRINTED" && tag.Status != "OUT")
                {
                    DailyFileLogger.Warn($"RegisterAsync gagal: Tag {tag.TagId} status tidak valid");
                    throw new Exception($"Tag {tag.TagId} tidak bisa diregister");
                }

                tag.Status = "STANDBY";
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.Histories.Add(new HistoryPrint
                {
                    Id = Guid.NewGuid().ToString(),
                    TagId = tag.Id,
                    ItemId = tag.ItemId,
                    Type = "REGISTER_TAG",
                    Reference = reference,
                    Action = "STANDBY",
                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                });
                DailyFileLogger.Info($"Tag {tag.TagId} berhasil diregister.");
            }

            await _db.SaveChangesAsync();

            DailyFileLogger.Info($"RegisterAsync berhasil. Reference: {reference}");
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di RegisterAsync.", ex);
            throw;
        }
    }

    public async Task<List<PrintHistoryResponseDto>> GetAvailableTagsAsync()
    {
        try
        {
        var result = await _db.Tags
            .Where(t => t.Status == "PRINTED" || t.Status == "OUT")
            .Join(_db.Items,
                t => t.ItemId,
                i => i.Id,
                (t, i) => new { t, i })
            .Join(_db.Histories.Where(h => h.Type == "PRINT"),
                ti => ti.t.TagId,
                h => h.TagId,
                (ti, h) => new PrintHistoryResponseDto
                {
                    TagId = ti.t.TagId,
                    ItemId = ti.t.ItemId,
                    ItemName = ti.i.Name,
                    Status = ti.t.Status,
                    BatchNo = h.Reference,
                    CreatedAt = h.CreatedAt
                })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        DailyFileLogger.Info($"GetAvailableTagsAsync berhasil. Total data: {result.Count}");

        return result;
    }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Error di GetAvailableTagsAsync.", ex);
            throw;
        }
    }
}