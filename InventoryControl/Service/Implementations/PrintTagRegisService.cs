namespace InventoryControl.Service.Implementations;

using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

public class PrintTagRegisService : IPrintTagRegisService
{
    private readonly AppDBContext _db;
    private readonly IConfiguration _config;

    private const string STAGING_LOCATION = "STAGING";

    public PrintTagRegisService(
        AppDBContext db,
        IConfiguration config
    )
    {
        _db = db;
        _config = config;
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

            ValidatePrintRequest(dto);

            var item = await GetItemAsync(dto.ItemId);

            var location =
                await GetStagingLocationAsync();

            var tags = new List<Tag>();

            var histories =
                new List<HistoryPrint>();

            var printCommands =
                new List<string>();

            using var trx =
                await _db.Database
                    .BeginTransactionAsync();

            try
            {
                var currentNumber =
                    await GetCurrentRunningNumberAsync();

                for (int i = 0; i < dto.Qty; i++)
                {
                    currentNumber++;

                    var tag = CreateTag(
                        item,
                        location,
                        currentNumber,
                        user
                    );

                    tags.Add(tag);

                    histories.Add(
                        CreatePrintHistory(
                            tag,
                            batchNo,
                            user
                        )
                    );

                    printCommands.Add(
                        BuildSBPL(
                            qrTag: tag.TagId,
                            epcTag:tag.EpcTag,
                            printDate:
                                DateTime.Now
                                    .ToString(
                                        "dd/MM/yyyy"
                                    ),
                            itemName:
                                item.Name,
                            itemId:
                                item.ItmId,
                            itemDesc:
                                item.Description
                        )
                    );
                }

                _db.Tags.AddRange(tags);

                _db.Histories.AddRange(
                    histories
                );

                await _db.SaveChangesAsync();

                await trx.CommitAsync();
            }
            catch
            {
                await trx.RollbackAsync();
                throw;
            }

            foreach (var cmd in printCommands)
            {
                await PrintToPrinterAsync(
                    cmd,
                    user
                );
            }

            DailyFileLogger.Info(
                $"Successfully printed {dto.Qty} tag(s).",
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

    public async Task RegisterAsync(
        TagRegistrationDto dto,
        string user
    )
    {
        try
        {
            if (!dto.TagIds.Any())
            {
                throw new Exception(
                    "Tag list cannot be empty."
                );
            }

            var tagIds = dto.TagIds;
            var tags = await _db.Tags
                .Where(t =>
                    EF.Constant(tagIds).Contains(t.EpcTag) ||
                    EF.Constant(tagIds).Contains(t.TagId)
                )
                .ToListAsync();

            if (!tags.Any())
            {
                throw new Exception(
                    "Tags not found."
                );
            }

            var reference =
                $"REG-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var histories =
                new List<HistoryPrint>();

            foreach (var tag in tags)
            {
                ValidateRegistrationStatus(tag);

                tag.Status = TagStatus.STANDBY;

                tag.UpdatedBy = user;

                tag.UpdatedAt = DateTime.UtcNow;

                histories.Add(CreateRegisterHistory(
                        tag,
                        reference,
                        user
                    )
                );
            }

            _db.Histories.AddRange( histories );

            await _db.SaveChangesAsync();

            DailyFileLogger.Info(
                $"Successfully registered {tags.Count} tag(s).", user
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred during tag registration process.",ex, user
            );

            throw;
        }
    }

    private void ValidatePrintRequest(PrintTagDto dto)
    {
        if (dto.Qty <= 0)
        {
            throw new Exception(
                "Quantity must be greater than zero."
            );
        }
    }

    private async Task<Item> GetItemAsync( string itemId )
    {
        var item = await _db.Items
            .FirstOrDefaultAsync(x =>
                x.Id == itemId &&
                !x.IsDelete
            );

        if (item == null)
        {
            throw new Exception(
                "Item not found."
            );
        }

        return item;
    }

    private async Task<Location>GetStagingLocationAsync()
    {
        var location = await _db.Locations
            .FirstOrDefaultAsync(x =>
                x.LocId == STAGING_LOCATION &&
                !x.IsDelete
            );

        if (location == null)
        {
            throw new Exception("STAGING location not found.");
        }

        return location;
    }

    private string GenerateEpcTag(Item item, long runningNumber)
    {
        var itemNumber = new string(item.ItmId.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(itemNumber))
            itemNumber = "0";

        return $"30{long.Parse(itemNumber):D6}{runningNumber:D16}";
    }

    private async Task<long> GetCurrentRunningNumberAsync()
    {
        var lastTag = await _db.Tags
            .OrderByDescending(x => x.TagId)
            .FirstOrDefaultAsync();

        if (lastTag == null)
            return 0;

        return long.Parse(lastTag.TagId.Substring(3));
    }

    private async Task PrintToPrinterAsync(
        string sbpl,
        string user
    )
    {
        var printerName = _config["PrinterSettings:PrinterName"];

        await RawPrinterHelper.SendStringToPrinterAsync( printerName,sbpl);

        await Task.CompletedTask;
    }

    private Tag CreateTag(
        Item item,
        Location location,
        long runningNumber,
        string user
    )
    {
        return new Tag
        {
            Id = Guid.NewGuid().ToString(),
            TagId = $"TAG{runningNumber:D5}",
            EpcTag = GenerateEpcTag(item, runningNumber),
            ItemId = item.Id,
            LocationId = location.Id,
            Status = TagStatus.PRINTED,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };
    }

    private HistoryPrint CreatePrintHistory(
            Tag tag,
            string batchNo,
            string user
        )
    {
        return new HistoryPrint
        {
            Id = Guid.NewGuid().ToString(),
            TagId = tag.Id,
            ItemId = tag.ItemId,
            Type = HistoryType.PRINT,
            Reference = batchNo,
            Action = "CREATE",
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };
    }

    private HistoryPrint CreateRegisterHistory(
            Tag tag,
            string reference,
            string user
        )
    {
        return new HistoryPrint
        {
            Id = Guid.NewGuid().ToString(),
            TagId = tag.Id,
            ItemId = tag.ItemId,
            Type = HistoryType.REGISTER_TAG,
            Reference = reference,
            Action = "STANDBY",
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };
    }

    private void ValidateRegistrationStatus(Tag tag)
    {
        if (
            tag.Status != TagStatus.PRINTED &&
            tag.Status != TagStatus.OUT
        )
        {
            throw new Exception( $"Tag '{tag.TagId}' cannot be registered." );
        }
    }

    private const string SBPL_TEMPLATE = @"
A
A3V+00000H+0000CS6#F5A1V00384H0913
ZAPSWKpercobaan1 
IP0e:h,epc:{epcTag},fsw:1;
%0H0425V00303P02
RH0,SATO0.ttf,0,034,034,SATO LABEL SOLUTIONS
%0H0656V001162D30,L,07,1,0
DN0009,{qrTag}
%0H0083V00303P02
RH0,SATO0.ttf,0,034,030,{printDate}    
%0H0684V00053P02
RH0,SATO0.ttf,0,040,042,1 UNIT
%0H0083V00275P02
RH0,SATO0.ttf,0,022,025,Made in Indonesia
%0H0083V00053P02
RH0,SATO0.ttf,0,040,042,{itemName}
%0H0083V00107P02
RH0,SATO0.ttf,0,041,034,{itemId}
%0H0083V00150P02
RH0,SATO0.ttf,0,040,030,{itemDesc}
Q1Z";

    private string BuildSBPL(
        string epcTag,
        string qrTag,
        string printDate,
        string itemName,
        string itemId,
        string itemDesc
    )
    {
        return SBPL_TEMPLATE
            .Replace("{epcTag}", epcTag)
            .Replace("{qrTag}", qrTag)
            .Replace("{printDate}", printDate)
            .Replace("{itemName}", itemName)
            .Replace("{itemId}", itemId)
            .Replace("{itemDesc}", itemDesc);
    }


    public async Task<List<PrintHistoryResponseDto>> GetAvailableTagsAsync()
    {
        try
        {
            DailyFileLogger.Info("Retrieving available tags.");

            var result = await _db.Tags
                .AsNoTracking()
                .Where(t =>
                    t.Status == TagStatus.PRINTED ||
                    t.Status == TagStatus.OUT
                )
                .Select(t =>
                    new PrintHistoryResponseDto
                    {
                        TagId = t.TagId,
                        ItemId = t.ItemId,
                        ItemName = t.Item.Name,
                        Status = t.Status.ToString(),

                        BatchNo =
                            _db.Histories
                                .Where(h =>
                                    h.TagId == t.Id &&
                                    h.Type == HistoryType.PRINT
                                )
                                .Select(h =>
                                    h.Reference
                                )
                                .FirstOrDefault(),

                        CreatedAt =
                            _db.Histories
                                .Where(h =>
                                    h.TagId == t.Id &&
                                    h.Type == HistoryType.PRINT
                                )
                                .Select(h =>
                                    h.CreatedAt
                                )
                                .FirstOrDefault()
                    }
                )
                .OrderByDescending(x =>
                    x.CreatedAt
                )
                .ToListAsync();

            DailyFileLogger.Info($"Retrieved {result.Count} available tag(s).");

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Failed to retrieve available tags.", ex);

            throw;
        }
    }


    public async Task<List<TagResponseDto>> GetAllAsync()
    {
        try
        {
            DailyFileLogger.Info("Retrieving all active tags.");

            var data = await _db.Tags
                .AsNoTracking()
                .Where(t => !t.IsDelete && (t.Status == TagStatus.PRINTED || t.Status == TagStatus.OUT))
                .Select(t =>
                    new TagResponseDto
                    {
                        TagId = t.TagId,
                        ItemName = t.Item.Name,
                        Epc = t.EpcTag,
                        Location = t.Location != null
                                ? t.Location.Name
                                : "-",
                        Status = t.Status.ToString()
                    }
                )
                .ToListAsync();

            DailyFileLogger.Info($"Retrieved {data.Count} tag(s).");

            return data;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Failed to retrieve tags.", ex);

            throw;
        }
    }

    public async Task<List<StockResponseDto>> GetStockPerItemAsync()
    {
        try
        {
            DailyFileLogger.Info("Retrieving stock summary.");

            var data = await _db.Tags
                .AsNoTracking()
                .Where(t =>
                    !t.IsDelete &&
                    t.Status == TagStatus.IN_STOCK
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
                        ItemId =
                            g.Key.ItemId,

                        ItemName =
                            g.Key.Name,

                        TotalStock =
                            g.Count()
                    }
                )
                .OrderBy(x =>
                    x.ItemName)
                .ToListAsync();

            DailyFileLogger.Info($"Retrieved stock summary for {data.Count} item(s).");

            return data;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("Failed to retrieve stock summary.", ex);

            throw;
        }
    }

    public async Task<StockQRDto?> GetByQRAsync(string tagId)
    {
        try
        {
            DailyFileLogger.Info($"Retrieving stock information for TagId '{tagId}'.");

            var result = await _db.Tags
                .AsNoTracking()
                .Where(t =>
                    t.TagId == tagId &&
                    !t.IsDelete
                )
                .Select(t =>
                    new StockQRDto
                    {
                        TagId = t.TagId,
                        ItemName =
                            t.Item.Name,
                        Location =
                            t.Location != null
                                ? t.Location.Name
                                : "-",
                        TotalStock =
                            _db.Tags.Count(x =>
                                x.ItemId ==
                                    t.ItemId &&
                                x.Status ==
                                    TagStatus.IN_STOCK &&
                                !x.IsDelete
                            )
                    }
                )
                .FirstOrDefaultAsync();

            if (result == null)
            {
                DailyFileLogger.Warn($"Tag '{tagId}' not found.");

                return null;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved stock information for TagId '{tagId}'."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"Failed to retrieve stock information for TagId '{tagId}'.",
                ex
            );

            throw;
        }
    }
}