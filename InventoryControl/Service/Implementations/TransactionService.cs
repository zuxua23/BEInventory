namespace InventoryControl.Service.Implementations;

using System.Text;
using ClosedXML.Excel;
using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;

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
        string? txType
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

            var query =
                from t in _db.Transactions

                join td in _db.TransactionDetails
                    on t.TrsId equals td.TrsId

                join tag in _db.Tags
                    on td.TagId equals tag.Id

                join item in _db.Items
                    on td.ItemId equals item.Id

                join reader in _db.Readers
                    on t.ReaderId equals reader.Id
                    into readerJoin

                from reader in readerJoin.DefaultIfEmpty()

                join d in _db.DOs
                    on t.ReferenceId equals d.DoId
                    into doJoin

                from d in doJoin.DefaultIfEmpty()

                join loc in _db.Locations
                    on tag.LocationId equals loc.Id
                    into locJoin

                from loc in locJoin.DefaultIfEmpty()

                select new TransactionHistoryDto
                {
                    TxDate = t.CreatedAt,
                    TxType = t.TrsType,

                    DoNumber =
                        d != null
                            ? d.DoNumber
                            : "-",

                    ReaderName =
                        reader != null
                            ? reader.Name
                            : "-",

                    TagId = tag.TagId,
                    ItemName = item.Name,

                    LocationName =
                        loc != null
                            ? loc.Name
                            : "-"
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
                query = query.Where(x =>
                    x.TxType == txType
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

    public async Task<byte[]> ExportExcel(
        DateTime? fromDate,
        DateTime? toDate,
        string? txType
    )
    {
        try
        {
            DailyFileLogger.Info(
                "Starting transaction history Excel export."
            );

            var data = await GetHistory(
                fromDate,
                toDate,
                txType
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
                "Tag ID",
                "Item",
                "Location"
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
                        "yyyy-MM-dd"
                    ) ?? "-";

                ws.Cell(row, 2).Value =
                    d.TxType;

                ws.Cell(row, 3).Value =
                    d.DoNumber;

                ws.Cell(row, 4).Value =
                    d.ReaderName;

                ws.Cell(row, 5).Value =
                    d.TagId;

                ws.Cell(row, 6).Value =
                    d.ItemName;

                ws.Cell(row, 7).Value =
                    d.LocationName;

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
                performedBy: "SYSTEM",
                description:
                    $"Exported {data.Count} transaction history record(s) to Excel."
            );

            return stream.ToArray();
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error(
                "An error occurred during Excel export.",
                ex
            );

            throw;
        }
    }

    public async Task<byte[]> ExportCsv(
        DateTime? fromDate,
        DateTime? toDate,
        string? txType
    )
    {
        try
        {
            DailyFileLogger.Info(
                "Starting transaction history CSV export."
            );

            var data = await GetHistory(
                fromDate,
                toDate,
                txType
            );

            var sb = new StringBuilder();

            sb.AppendLine(
                "Date,Type,DO Number,Reader,Tag ID,Item,Location"
            );

            string Escape(string val) =>
                $"\"{val?.Replace("\"", "\"\"")}\"";

            foreach (var d in data)
            {
                sb.AppendLine(
                    $"{d.TxDate:yyyy-MM-dd}," +
                    $"{Escape(d.TxType)}," +
                    $"{Escape(d.DoNumber)}," +
                    $"{Escape(d.ReaderName)}," +
                    $"{Escape(d.TagId)}," +
                    $"{Escape(d.ItemName)}," +
                    $"{Escape(d.LocationName)}"
                );
            }

            DailyFileLogger.Info(
                $"CSV export completed successfully. TotalRows='{data.Count}'."
            );

            DailyFileLogger.Audit(
                action: "EXPORT_CSV",
                entity: "TRANSACTION_HISTORY",
                entityId: "TRANSACTION_EXPORT",
                performedBy: "SYSTEM",
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
                ex
            );

            throw;
        }
    }
}