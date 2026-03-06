using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
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
        if (dto.Qty <= 0)
            throw new Exception("Qty harus lebih dari 0");

        var batchNo = $"PRN-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var item = await _db.Items.FirstOrDefaultAsync(x => x.Id == dto.ItemId);

        if (item == null)
            throw new Exception("Item tidak ditemukan");

        var printerName = "SATO CL4NX Plus (SmaPri)";

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
                throw new Exception("Location STAGING tidak ditemukan");

            var tag = new Tag
            {
                Id = Guid.NewGuid().ToString(),
                TagId = tagId,
                EpcTag = epc,
                ItemId = dto.ItemId,
                LocationId= location.Id,
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

            bool printed = PrinterHelper.SendStringToPrinter(printerName, sbpl);
            Console.WriteLine(sbpl);
            if (!printed)
            {
                throw new Exception("Gagal mengirim data ke printer SATO");
            }
        }

        await _db.SaveChangesAsync();

        return batchNo;
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
            if (!dto.TagIds.Any())
                throw new Exception("Tag tidak boleh kosong");

            var tags = await _db.Tags
                .Where(t => dto.TagIds.Contains(t.TagId))
                .ToListAsync();
        if(!tags.Any())
            throw new Exception("Tag tidak ditemukan");

        var foundTagIds = tags.Select(t => t.TagId).ToHashSet();
        var missingTags = dto.TagIds.Where(id => !foundTagIds.Contains(id)).ToList();

        if (missingTags.Any())
            throw new Exception($"Tag tidak ditemukan: {string.Join(",", missingTags)}");

        var reference = $"REG-{DateTime.UtcNow:yyyyMMddHHmmss}";


        foreach (var tag in tags)
            {
                if (tag.Status != "PRINTED" && tag.Status != "OUT")
                    throw new Exception($"Tag {tag.TagId} tidak bisa diregister");

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
            }

            await _db.SaveChangesAsync();
        }

    public async Task<List<PrintHistoryResponseDto>> GetAvailableTagsAsync()
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

        return result;
    }
}