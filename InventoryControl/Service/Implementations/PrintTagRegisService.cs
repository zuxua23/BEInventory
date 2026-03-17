using InTheHand.Net.Bluetooth;
//using InventoryControl.Constans;
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
        Console.WriteLine("=== PRINT ASYNC KEPANGGIL ===");

        try
        {
            if (dto.Qty <= 0)
                throw new Exception("Qty harus lebih dari 0");

            var batchNo = $"PRN-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var item = await _db.Items.FirstOrDefaultAsync(x => x.Id == dto.ItemId);
            if (item == null)
                throw new Exception("Item tidak ditemukan");

            var lastTag = await _db.Tags
                .OrderByDescending(t => t.TagId)
                .FirstOrDefaultAsync();

            var lastNumber = await _db.Tags.CountAsync();
            if (lastTag != null)
                lastNumber = int.Parse(lastTag.TagId.Substring(3));

            var location = await _db.Locations
                .FirstOrDefaultAsync(x => x.LocId == "STAGING");

            if (location == null)
                throw new Exception("Location STAGING tidak ditemukan");

            var printerIp = "172.20.10.3";
            var port = 9100;

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "FilePrint",
                "label.prn"
            );
            for (int i = 0; i < dto.Qty; i++)
            {
                lastNumber++;

                var tagId = $"TAG{lastNumber:D5}";
                var epc = $"A{item.ItmId}{lastNumber:D10}";
                var qr = tagId;

                var sbpl = BuildSBPL(epc, item.Name, qr, dto.Qty);

                var bytes = SBPLStringToBytes(sbpl);

                Console.WriteLine("=== SBPL ===");
                Console.WriteLine(sbpl);
                Console.WriteLine("HEX:");
                Console.WriteLine(string.Join(" ", bytes.Select(b => b.ToString("X2"))));

                bool printed = PW4NX_Helper.SendToPrinter("172.20.10.3", 9100, bytes);

                if (!printed)
                {
                    throw new Exception("Gagal print ke SATO");
                }

                Console.WriteLine($"PRINT SUCCESS: {tagId}");


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

                await _db.SaveChangesAsync();
            }

            Console.WriteLine("=== PRINT SELESAI ===");
            return batchNo;
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR PRINT: " + ex.Message);
            throw;
        }
    }

    //private byte[] LoadPrnAndReplace(string filePath, string itemName, string qr, int qty)
    //{
    //    var content = File.ReadAllText(filePath);

    //    content = content.Replace("[item_no]", itemName);
    //    content = content.Replace("[qr_data]", qr);
    //    content = content.Replace("[qty]", qty.ToString());

    //    return Encoding.ASCII.GetBytes(content);
    //}
    private const string SBPL_TEMPLATE = @"
A
%1
H0040
V00666
2D30,L,06,1,0
DN0009,{QR}
%1
H0053
V00501
P02
RH0,SATO0.ttf,0,063,069,Item No : {ITEM}
Q1
Z";

    private string BuildSBPL(string epc, string itemName, string qr, int qty)
    {
        return SBPL_TEMPLATE
            .Replace("{EPC}", epc)
            .Replace("{ITEM}", itemName)
            .Replace("{QR}", qr)
            .Replace("{QTY}", qty.ToString());
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


    //private string BuildSBPL(string epc, string itemName, string qr)
    //{
    //    return $@"AA3V+00000H+0000CS6#F5A1V00901H0300ZAPSWKlabel_ajgIP0e:h,epc:A00000000000000000000001,fsw:1;%1H0040V008662D30,L,06,1,0DN0009,[qr_data]%1H0053V00701P02RH0,SATO0.ttf,0,022,025,Item No : [item_no]%1H0096V00699P02RH0,SATO0.ttf,0,022,025,Qty : [qty]Q1Z";
    //}


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