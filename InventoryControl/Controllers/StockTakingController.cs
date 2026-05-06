namespace InventoryControl.Controllers;

using DocumentFormat.OpenXml.Packaging;
using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/stock-taking")]
public class StockTakingController : ControllerBase
{
    private readonly IStockTakingService _service;

    public StockTakingController(IStockTakingService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(StockTakingCreateDto dto)
    {
        try
        {
            var user = User.Identity?.Name ?? "system";

            var id = await _service.CreateAsync(dto, user);

            return Ok(new { stockTakingId = id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message,
                detail = ex.InnerException?.Message
            });
        }
    }

    [HttpGet]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetStockData()
    {
        var data = await _service.GetStockDataAsync();
        return Ok(data);
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan(StockTakingScanDto dto)
    {
        await _service.ScanAsync(dto);
        return Ok(new { message = "Tag discan" });
    }
    [HttpPost("scan/bulk")]
    public async Task<IActionResult> Bulk(StockTakingBulkScanDto dto)
    {
        await _service.BulkScanAsync(dto);
        return Ok();
    }
    [HttpGet("compare/{id}")]
    public async Task<IActionResult> Compare(string id)
    {
        var data = await _service.GetCompareAsync(id);
        return Ok(data);
    }


    [HttpPost("remove")]
    public async Task<IActionResult> Remove(StockTakingRemoveDto dto)
    {
        await _service.RemoveAsync(dto);
        return Ok(new { message = "Tag ditandai remove" });
    }

    [HttpPost("manual-add")]
    public async Task<IActionResult> ManualAdd(StockTakingManualAddDto dto)
    {
        await _service.ManualAddAsync(dto);
        return Ok(new { message = "Manual add dicatat" });
    }

    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize(StockTakingFinalizeDto dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.FinalizeAsync(dto, user);
        return Ok(new { message = "Stock Taking selesai" });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var data = await _service.GetActiveAsync();
        return Ok(data);
    }

    [HttpGet("system/{sttId}")]
    public async Task<IActionResult> GetSystem(string sttId)
    {
        var data = await _service.GetSystemDataAsync(sttId);
        return Ok(data);
    }

    [HttpGet("export/system/excel")]
    public async Task<IActionResult> ExportSystemExcel(string sttId)
    {
        var file = await _service.ExportSystemExcelAsync(sttId);

        var fileName = $"StockTaking-System-{DateTime.Now:yyyyMMddHHmmss}.xlsx";


        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }

    [HttpGet("export/system/csv")]
    public async Task<IActionResult> ExportSystemCsv(string sttId)
    {
        var csv = await _service.ExportSystemCsvAsync(sttId);

        var fileName = $"StockTaking-System-{DateTime.Now:yyyyMMddHHmmss}.csv";

        return File(
            Encoding.UTF8.GetBytes(csv),
            "text/csv",
            fileName
        );
    }

    [HttpGet("export/scan/excel")]
    public async Task<IActionResult> ExportScanExcel(string sttId)
    {
        var file = await _service.ExportCompareExcelAsync(sttId);

        var fileName = $"StockTaking-Scan-{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }

    [HttpGet("export/scan/csv")]
    public async Task<IActionResult> ExportScanCsv(string sttId)
    {
        var csv = await _service.ExportCompareCsvAsync(sttId);
        var fileName = $"StockTaking-Scan-{DateTime.Now:yyyyMMddHHmmss}.csv";

        return File(
            Encoding.UTF8.GetBytes(csv),
            "text/csv",
            fileName
        );
    }

}