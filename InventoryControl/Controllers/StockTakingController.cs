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
    [AuthorizePermissionHybrid("STOCK_TAKING_CREATE")]
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

    [HttpGet("loc")]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetLocData()
    {
        var data = await _service.GetLocAsync();
        return Ok(data);
    }

    [HttpGet]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetStockData()
    {
        var data = await _service.GetStockDataAsync();
        return Ok(data);
    }

    [HttpPost("scan")]
    [AuthorizePermissionHybrid("STOCK_TAKING_SCAN")]

    public async Task<IActionResult> Scan(StockTakingScanDto dto)
    {
        await _service.ScanAsync(dto);
        return Ok(new { message = "Tag discan" });
    }

    [HttpPost("scan/bulk")]
    [AuthorizePermissionHybrid("STOCK_TAKING_SCAN")]
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
    [AuthorizePermissionHybrid("STOCK_TAKING_REMOVE")]
    public async Task<IActionResult> Remove(StockTakingRemoveDto dto)
    {
        await _service.RemoveAsync(dto);
        return Ok(new { message = "Tag ditandai remove" });
    }

    [HttpPost("manual-add")]
    [AuthorizePermissionHybrid("STOCK_TAKING_MANUAL")]
    public async Task<IActionResult> ManualAdd(StockTakingManualAddDto dto)
    {
        await _service.ManualAddAsync(dto);
        return Ok(new { message = "Manual add dicatat" });
    }

    [HttpPost("finalize")]
    [AuthorizePermissionHybrid("STOCK_TAKING_FINALIZE")]
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

    [HttpGet("tags/{sttId}")]
    [AuthorizePermissionHybrid("STOCK_TAKING_SCAN")]
    public async Task<IActionResult> GetSessionTags(string sttId)
    {
        try
        {
            var data = await _service.GetSessionTagsAsync(sttId);
            return Ok(data);
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
    [HttpGet("available-tags/{sttId}")]
    [AuthorizePermissionHybrid("STOCK_TAKING_SCAN")]
    public async Task<IActionResult> GetAvailableTags(string sttId)
    {
        var data = await _service.GetAvailableTagsAsync(sttId);
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
    [HttpGet("progress/{sttId}")]
    public async Task<IActionResult> GetProgress(string sttId)
    {
        var data = await _service.GetProgressAsync(sttId);
        return Ok(data);
    }

}
