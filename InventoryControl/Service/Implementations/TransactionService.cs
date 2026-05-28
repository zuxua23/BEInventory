namespace InventoryControl.Service.Implementations;

using ClosedXML.Excel;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Models;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class TransactionService : ITransactionService
{
    private readonly AppDBContext _db;

    public TransactionService(AppDBContext db)
    {
        _db = db;
    }

    public async Task<List<TransactionHistoryDto>> GetHistory(
        DateTime? fromDate,
        DateTime? toDate,
        string? txType,
        string? keyword
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving transaction history. " +
                $"FromDate='{fromDate}', " +
                $"ToDate='{toDate}', " +
                $"TransactionType='{txType ?? "ALL"}'."
            );
            if (fromDate.HasValue &&
                toDate.HasValue &&
                toDate < fromDate)
            {
                throw new Exception(
                    "To Date cannot be less than From Date"
                );
            }
            var query =
                from t in _db.Transactions

                join reader in _db.Readers
                    on t.ReaderId equals reader.Id
                    into readerJoin
                from reader in readerJoin.DefaultIfEmpty()

                join d in _db.DOs
                    on t.ReferenceId equals d.DoId
                    into doJoin
                from d in doJoin.DefaultIfEmpty()

                select new TransactionHistoryDto
                {
                    Id = t.TrsId,
                    TxDate = t.CreatedAt,
                    TrsType = t.TrsType,
                    TxType = t.TrsType.ToString(),

                    DoNumber =
                        d != null
                            ? d.DoNumber
                            : "-",

                    ReaderName =
                        reader != null
                            ? reader.Name
                            : "-",

                    TotalTag = _db.TransactionDetails
                        .Count(td => td.TrsId == t.TrsId)
                };

            if (fromDate.HasValue)
            {
                query = query.Where(x =>
                    x.TxDate >= fromDate
                );
            }

            if (toDate.HasValue)
            {
                query = query.Where(x =>
                    x.TxDate <= toDate
                );
            }

            if (!string.IsNullOrEmpty(txType))
            {
                if (Enum.TryParse<TransactionType>(
                    txType,
                    out var parsedType
                ))
                {
                    query = query.Where(x =>
                        x.TrsType == parsedType
                    );
                }
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();

                query = query.Where(x =>
                    (x.DoNumber != null &&
                     x.DoNumber.ToLower().Contains(keyword))

                    || (x.ReaderName != null &&
                        x.ReaderName.ToLower().Contains(keyword))

                    || (x.TxType != null &&
                        x.TxType.ToLower().Contains(keyword))
                );
            }

            var result = await query
                .OrderByDescending(x => x.TxDate)
                .ToListAsync();

            DailyFileLogger.Info(
                $"Successfully retrieved {result.Count} transaction history record(s)."
            );

            return result;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred while retrieving transaction history.",
                ex
            );

            throw;
        }
    }

    public async Task<TransactionHistoryDetailResponseDto?> GetDetail(string id)
    {
        try
        {
            DailyFileLogger.Info(
                $"Retrieving transaction detail with ID '{id}'."
            );

            var header =
                await (
                    from t in _db.Transactions

                    join reader in _db.Readers
                        on t.ReaderId equals reader.Id
                        into readerJoin
                    from reader in readerJoin.DefaultIfEmpty()

                    join d in _db.DOs
                        on t.ReferenceId equals d.DoId
                        into doJoin
                    from d in doJoin.DefaultIfEmpty()

                    where t.TrsId == id

                    select new TransactionHistoryDetailResponseDto
                    {
                        Id = t.TrsId,
                        TxDate = t.CreatedAt,
                        TxType = t.TrsType.ToString(),

                        DoNumber =
                            d != null
                                ? d.DoNumber
                                : "-",

                        ReaderName =
                            reader != null
                                ? reader.Name
                                : "-"
                    }
                )
                .FirstOrDefaultAsync();

            if (header == null)
                return null;

            header.Details =
                await (
                    from td in _db.TransactionDetails

                    join tag in _db.Tags
                        on td.TagId equals tag.Id

                    join item in _db.Items
                        on td.ItemId equals item.Id

                    join loc in _db.Locations
                        on tag.LocationId equals loc.Id
                        into locJoin
                    from loc in locJoin.DefaultIfEmpty()

                    where td.TrsId == id

                    select new TransactionHistoryDetailDto
                    {
                        TagId = tag.TagId,
                        ItemName = item.Name,

                        LocationName =
                            loc != null
                                ? loc.Name
                                : "-"
                    }
                )
                .ToListAsync();

            header.TotalTag = header.Details.Count;

            if (header == null)
            {
                DailyFileLogger.Warn(
                    $"Transaction with ID '{id}' was not found."
                );

                return null;
            }

            DailyFileLogger.Info(
                $"Successfully retrieved transaction detail with ID '{id}'."
            );

            return header;
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                $"An error occurred while retrieving transaction detail with ID '{id}'.",
                ex
            );

            throw;
        }
    }

    public async Task<byte[]> ExportExcel(
        DateTime? fromDate,
        DateTime? toDate,
        string? txType,
        string? keyword,
        string exportBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Starting transaction history Excel export. ExportBy='{exportBy}'."
            );

            var data = await GetHistory(
                fromDate,
                toDate,
                txType,
                keyword
            );

            using var wb = new XLWorkbook();

            var ws = wb.Worksheets.Add(
                "Transactions"
            );

            var headers = new[]
            {
                "Date",
                "Type",
                "DO Number",
                "Reader",
                "Total Tag"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value =
                    headers[i];
            }

            var headerRange = ws.Range(
                1,
                1,
                1,
                headers.Length
            );

            headerRange.Style.Font.Bold = true;

            headerRange.Style.Fill
                .BackgroundColor =
                    XLColor.FromHtml("#3b82f6");

            headerRange.Style.Font.FontColor =
                XLColor.White;

            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            int row = 2;

            foreach (var d in data)
            {
                ws.Cell(row, 1).Value =
                    d.TxDate?.ToString(
                        "dd/MM/yyyy"
                    ) ?? "-";

                ws.Cell(row, 2).Value =
                    d.TxType;

                ws.Cell(row, 3).Value =
                    d.DoNumber;

                ws.Cell(row, 4).Value =
                    d.ReaderName;

                ws.Cell(row, 5).Value =
                    d.TotalTag;


                row++;
            }

            ws.Columns()
                .AdjustToContents();

            var tableRange = ws.Range(
                1,
                1,
                row - 1,
                headers.Length
            );

            tableRange.Style.Border
                .OutsideBorder =
                    XLBorderStyleValues.Thin;

            tableRange.Style.Border
                .InsideBorder =
                    XLBorderStyleValues.Thin;

            for (int i = 2; i < row; i++)
            {
                if (i % 2 == 0)
                {
                    ws.Row(i).Style.Fill
                        .BackgroundColor =
                            XLColor.FromHtml(
                                "#f8fafc"
                            );
                }
            }

            using var stream =
                new MemoryStream();

            wb.SaveAs(stream);

            DailyFileLogger.Info(
                $"Excel export completed successfully. TotalRows='{data.Count}'."
            );

            DailyFileLogger.Audit(
                action: "EXPORT_EXCEL",
                entity: "TRANSACTION_HISTORY",
                entityId: "TRANSACTION_EXPORT",
                performedBy: exportBy,
                description:
                    $"Exported {data.Count} transaction history record(s) to Excel."
            );

            return stream.ToArray();
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred during Excel export.",
                ex,
                exportBy
            );

            throw;
        }
    }

    public async Task<byte[]> ExportCsv(
        DateTime? fromDate,
        DateTime? toDate,
        string? txType,
        string? keyword,
        string exportBy
    )
    {
        try
        {
            DailyFileLogger.Info(
                $"Starting transaction history CSV export. ExportBy='{exportBy}'."
            );

            var data = await GetHistory(
                fromDate,
                toDate,
                txType,
                keyword
            );

            var sb = new StringBuilder();

            sb.AppendLine(
                "Date,Type,DO Number,Reader,Total Tag"
            );

            string Escape(string? val) =>
                $"\"{val?.Replace("\"", "\"\"") ?? "-"}\"";

            foreach (var d in data)
            {
                sb.AppendLine(
                    $"{d.TxDate:dd/MM/yyyy}," +
                    $"{Escape(d.TxType)}," +
                    $"{Escape(d.DoNumber)}," +
                    $"{Escape(d.ReaderName)}," +
                    $"{d.TotalTag}"
                );
            }

            DailyFileLogger.Info(
                $"CSV export completed successfully. TotalRows='{data.Count}'."
            );

            DailyFileLogger.Audit(
                action: "EXPORT_CSV",
                entity: "TRANSACTION_HISTORY",
                entityId: "TRANSACTION_EXPORT",
                performedBy: exportBy,
                description:
                    $"Exported {data.Count} transaction history record(s) to CSV."
            );

            return Encoding.UTF8.GetBytes(
                sb.ToString()
            );
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred during CSV export.",
                ex,
                exportBy
            );

            throw;
        }
    }
}