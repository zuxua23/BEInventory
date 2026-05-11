namespace InventoryControl.Service.Implementations;

using System.Text;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class PrintTagRegisService : IPrintTagRegisService
{
    private readonly AppDBContext _db;

    public PrintTagRegisService(AppDBContext db)
    {
        _db = db;
    }

    public async Task PrintAsync(
        PrintTagDto dto,
        string user,
        string batchNo
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Starting tag printing process. ItemId='{dto.ItemId}', Qty='{dto.Qty}', BatchNo='{batchNo}'.",
                user
            );

            if (dto.Qty <= 0)
            {
                DailyFileLogger.Warn(
                    "Print failed because quantity must be greater than zero.",
                    user
                );

                throw new Exception(
                    "Quantity must be greater than zero."
                );
            }

            var item = await _db.Items
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.ItemId &&
                    !x.IsDelete
                );

            if (item == null)
            {
                DailyFileLogger.Warn(
                    $"Item with ID '{dto.ItemId}' was not found.",
                    user
                );

                throw new Exception(
                    "Item not found."
                );
            }

            var lastTag = await _db.Tags
                .OrderByDescending(t => t.TagId)
                .FirstOrDefaultAsync();

            var lastNumber =
                await _db.Tags.CountAsync();

            if (lastTag != null)
            {
                lastNumber = int.Parse(
                    lastTag.TagId.Substring(3)
                );
            }

            var location = await _db.Locations
                .FirstOrDefaultAsync(x =>
                    x.LocId == "STAGING" &&
                    !x.IsDelete
                );

            if (location == null)
            {
                DailyFileLogger.Warn(
                    "STAGING location was not found.",
                    user
                );

                throw new Exception(
                    "STAGING location not found."
                );
            }

            for (int i = 0; i < dto.Qty; i++)
            {
                lastNumber++;

                var tagId =
                    $"TAG{lastNumber:D5}";

                var epc =
                    $"A{item.ItmId}{lastNumber:D10}";

                var qr = tagId;

                var sbpl = BuildSBPL(
                    epc,
                    item.ItmId,
                    qr,
                    dto.Qty
                );

                bool printed =
                    RawPrinterHelper
                        .SendStringToPrinter(
                            "SATO CL4NX Plus (SmaPri)",
                            sbpl
                        );

                if (!printed)
                {
                    DailyFileLogger.Error(
                        $"Printer failed while printing TagId '{tagId}'.",
                        null,
                        user
                    );

                    throw new Exception(
                        "Failed to print to SATO printer."
                    );
                }

                var tag = new Tag
                {
                    Id = Guid.NewGuid()
                        .ToString(),

                    TagId = tagId,
                    EpcTag = epc,
                    ItemId = dto.ItemId,

                    LocationId = location.Id,

                    Status = "PRINTED",

                    CreatedBy = user,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Tags.Add(tag);

                _db.Histories.Add(
                    new HistoryPrint
                    {
                        Id = Guid.NewGuid()
                            .ToString(),

                        TagId = tag.Id,
                        ItemId = dto.ItemId,

                        Type = "PRINT",

                        Reference = batchNo,

                        Action = "CREATE",

                        CreatedBy = user,
                        CreatedAt =
                            DateTime.UtcNow
                    }
                );

                await _db.SaveChangesAsync();

                DailyFileLogger.Info(
                    $"Tag successfully printed. TagId='{tagId}', EPC='{epc}'.",
                    user
                );

                DailyFileLogger.Audit(
                    action: "PRINT_TAG",
                    entity: "TAG",
                    entityId: tagId,
                    performedBy: user,
                    description:
                        $"Printed tag for item '{item.ItmId}' with batch '{batchNo}'."
                );
            }

            DailyFileLogger.Info(
                $"Tag printing completed successfully. BatchNo='{batchNo}'.",
                user
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred during tag printing process.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task<string> PrintBulkAsync(
        List<PrintTagDto> list,
        string user
    )
    {
        try
        {
            var batchNo =
                $"PRN-{DateTime.UtcNow:yyyyMMddHHmmss}";

            DailyFileLogger.Info(
                $"Starting bulk print process. BatchNo='{batchNo}', TotalItems='{list.Count}'.",
                user
            );

            foreach (var dto in list)
            {
                await PrintAsync(
                    dto,
                    user,
                    batchNo
                );
            }

            DailyFileLogger.Info(
                $"Bulk print completed successfully. BatchNo='{batchNo}'.",
                user
            );

            return batchNo;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred during bulk print process.",
                ex,
                user
            );

            throw;
        }
    }

    private const string SBPL_TEMPLATE = @"
A
%1
H0040
V00336
2D30,L,06,1,0
DN0009,{QR}
%1
H0053
V00201
P02
RH0,SATO0.ttf,0,028,031,ITEM : {ITEM}
Q1
Z
A
PH";

    private string BuildSBPL(
        string epc,
        string itemId,
        string qr,
        int qty
    )
    {
        return SBPL_TEMPLATE
            .Replace("{EPC}", epc)
            .Replace("{ITEM}", itemId)
            .Replace("{QR}", qr)
            .Replace("{QTY}", qty.ToString());
    }

    private byte[] SBPLStringToBytes(
        string sbpl
    )
    {
        var bytes = new List<byte>();

        foreach (char c in sbpl)
        {
            switch (c)
            {
                case '\u0002':
                    bytes.Add(0x02);
                    break;

                case '\u0003':
                    bytes.Add(0x03);
                    break;

                case '\u001B':
                    bytes.Add(0x1B);
                    break;

                default:
                    bytes.Add((byte)c);
                    break;
            }
        }

        return bytes.ToArray();
    }

    public async Task RegisterAsync(
        TagRegistrationDto dto,
        string user
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Starting tag registration process. TotalTags='{dto.TagIds.Count}'.",
                user
            );

            if (!dto.TagIds.Any())
            {
                DailyFileLogger.Warn(
                    "Tag registration failed because no tag IDs were provided.",
                    user
                );

                throw new Exception(
                    "Tag list cannot be empty."
                );
            }

            var tags = await _db.Tags
                .Where(t =>
                    dto.TagIds.Contains(
                        t.TagId
                    )
                )
                .ToListAsync();

            if (!tags.Any())
            {
                DailyFileLogger.Warn(
                    "No matching tags were found for registration.",
                    user
                );

                throw new Exception(
                    "Tags not found."
                );
            }

            var foundTagIds =
                tags.Select(t => t.TagId)
                    .ToHashSet();

            var missingTags =
                dto.TagIds
                    .Where(id =>
                        !foundTagIds.Contains(id)
                    )
                    .ToList();

            if (missingTags.Any())
            {
                DailyFileLogger.Warn(
                    $"Missing tags detected: {string.Join(",", missingTags)}.",
                    user
                );

                throw new Exception(
                    $"Tags not found: {string.Join(",", missingTags)}"
                );
            }

            var reference =
                $"REG-{DateTime.UtcNow:yyyyMMddHHmmss}";

            foreach (var tag in tags)
            {
                if (
                    tag.Status != "PRINTED" &&
                    tag.Status != "OUT"
                )
                {
                    DailyFileLogger.Warn(
                        $"Tag '{tag.TagId}' cannot be registered because status is '{tag.Status}'.",
                        user
                    );

                    throw new Exception(
                        $"Tag '{tag.TagId}' cannot be registered."
                    );
                }

                tag.Status = "STANDBY";
                tag.UpdatedBy = user;
                tag.UpdatedAt = DateTime.UtcNow;

                _db.Histories.Add(
                    new HistoryPrint
                    {
                        Id = Guid.NewGuid()
                            .ToString(),

                        TagId = tag.Id,
                        ItemId = tag.ItemId,

                        Type = "REGISTER_TAG",

                        Reference = reference,

                        Action = "STANDBY",

                        CreatedBy = user,
                        CreatedAt =
                            DateTime.UtcNow
                    }
                );

                DailyFileLogger.Info(
                    $"Tag '{tag.TagId}' successfully registered.",
                    user
                );

                DailyFileLogger.Audit(
                    action: "REGISTER_TAG",
                    entity: "TAG",
                    entityId: tag.TagId,
                    performedBy: user,
                    description:
                        $"Registered tag '{tag.TagId}' to STANDBY status."
                );
            }

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Tag registration completed successfully. Reference='{reference}'.",
                user
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred during tag registration process.",
                ex,
                user
            );

            throw;
        }
    }

    public async Task<List<PrintHistoryResponseDto>>
        GetAvailableTagsAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving available tags for registration."
            );

            var result = await _db.Tags
                .Where(t =>
                    t.Status == "PRINTED" ||
                    t.Status == "OUT"
                )
                .Join(
                    _db.Items,
                    t => t.ItemId,
                    i => i.Id,
                    (t, i) => new { t, i }
                )
                .Join(
                    _db.Histories.Where(h =>
                        h.Type == "PRINT"
                    ),
                    ti => ti.t.TagId,
                    h => h.TagId,
                    (ti, h) =>
                        new PrintHistoryResponseDto
                        {
                            TagId = ti.t.TagId,
                            ItemId = ti.t.ItemId,
                            ItemName = ti.i.Name,
                            Status = ti.t.Status,
                            BatchNo = h.Reference,
                            CreatedAt = h.CreatedAt
                        }
                )
                .OrderByDescending(x =>
                    x.CreatedAt
                )
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} available tag(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving available tags.",
                ex
            );

            throw;
        }
    }

    public async Task<List<TagResponseDto>>
        GetAllAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving all active tags."
            );

            var data = await _db.Tags
                .Where(t => t.isDelete == 0)
                .Include(t => t.Item)
                .Include(t => t.Location)
                .Select(t => new TagResponseDto
                {
                    TagId = t.TagId,

                    ItemName = t.Item.Name,
                    Epc = t.EpcTag,

                    Location =
                        t.Location != null
                            ? t.Location.Name
                            : "-",

                    Status = t.Status
                })
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {data.Count} tag(s)."
            );

            return data;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving tags.",
                ex
            );

            throw;
        }
    }

    public async Task<List<StockResponseDto>>
        GetStockPerItemAsync()
    {
        try
        {
            DailyFileLogger.Info(
                "Retrieving stock summary per item."
            );

            var data = await _db.Tags
                .Where(t =>
                    t.isDelete == 0 &&
                    t.Status == "IN_STOCK"
                )
                .GroupBy(t =>
                    new
                    {
                        t.ItemId,
                        t.Item.Name
                    }
                )
                .Select(g =>
                    new StockResponseDto
                    {
                        ItemId = g.Key.ItemId,
                        ItemName = g.Key.Name,
                        TotalStock = g.Count()
                    }
                )
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved stock summary for {data.Count} item(s)."
            );

            return data;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving stock summary.",
                ex
            );

            throw;
        }
    }

    public async Task<StockQRDto?> GetByQRAsync(
        string tagId
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving stock information for TagId '{tagId}'."
            );

            var tag = await _db.Tags
                .Include(t => t.Item)
                .Include(t => t.Location)
                .FirstOrDefaultAsync(t =>
                    t.TagId == tagId &&
                    t.isDelete == 0
                );

            if (tag == null)
            {
                DailyFileLogger.Warn(
                    $"Tag '{tagId}' was not found."
                );

                return null;
            }

            var totalStock =
                await _db.Tags
                    .Where(t =>
                        t.ItemId == tag.ItemId &&
                        t.Status == "IN_STOCK" &&
                        t.isDelete == 0
                    )
                    .CountAsync();

            var result = new StockQRDto
            {
                TagId = tag.TagId,

                ItemName = tag.Item?.Name,

                Location =
                    tag.Location != null
                        ? tag.Location.Name
                        : "-",

                TotalStock = totalStock
            };

            DailyFileLogger.Info(
                $"Successfully retrieved stock information for TagId '{tagId}'."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving stock information for TagId '{tagId}'.",
                ex
            );

            throw;
        }
    }
}