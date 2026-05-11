using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[ApiController]
[Route("api/transaction")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionController(ITransactionService service)
    {
        _service = service;
    }

    [HttpGet("history")]
    [AuthorizePermissionHybrid("TRANSACTION_GET")]
    public async Task<IActionResult> GetHistory(
        DateTime? fromDate,
        DateTime? toDate,
        string? txType, string? keyword)
    {
        var data = await _service.GetHistory(fromDate, toDate, txType, keyword);
        return Ok(data);
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel(
    DateTime? fromDate,
    DateTime? toDate,
    string? txType, string? keyword)
    {
        var file = await _service.ExportExcel(fromDate, toDate, txType, keyword );
        var datePart = "";

        if (fromDate.HasValue && toDate.HasValue)
            datePart = $"{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}_";

        var fileName = $"Transactions_{datePart}{(string.IsNullOrEmpty(txType) ? "all" : txType)}.xlsx";

        return File(file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
           fileName);
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(DateTime? fromDate, DateTime? toDate, string? txType, string? keyword)
    {
        var file = await _service.ExportCsv(fromDate, toDate, txType, keyword);

        var datePart = "";

        if (fromDate.HasValue && toDate.HasValue)
            datePart = $"{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}_";

        var fileName = $"Transactions_{datePart}{(string.IsNullOrEmpty(txType) ? "all" : txType)}.csv";

        return File(file, "text/csv", fileName);
    }
}

