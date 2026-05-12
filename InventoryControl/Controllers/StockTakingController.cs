namespace InventoryControl.Controllers;

using DocumentFormat.OpenXml.Packaging;
using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
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
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var data = await _service.GetActiveAsync();
        if (data == null) return NotFound(new { message = "Tidak ada sesi aktif" });
        return Ok(data);
    }

    [HttpGet("tags/{sttId}")]
    public async Task<IActionResult> GetSessionTags(string sttId)
    {
        try
        {
            var data = await _service.GetSessionTagsAsync(sttId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("GetSessionTags error", ex);
            return StatusCode(500, new { message = ex.Message });
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
    public async Task<IActionResult> Bulk(StockTakingBulkScanDto dto)
    {
        try
        {
            await _service.BulkScanAsync(dto);
            return Ok(new { message = "Bulk scan berhasil" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("progress/{sttId}")]
    public async Task<IActionResult> GetProgress(string sttId)
    {
        var data = await _service.GetProgressAsync(sttId);
        return Ok(data);
    }

    [HttpGet("compare/{id}")]
    public async Task<IActionResult> Compare(string id)
    {
        var data = await _service.GetCompareAsync(id);
        return Ok(data);
    }

    [HttpGet("system/{sttId}")]
    public async Task<IActionResult> GetSystem(string sttId)
    {
        var data = await _service.GetSystemDataAsync(sttId);
        return Ok(data);
    }

    [HttpPost("remove")]
    [AuthorizePermissionHybrid("STOCK_TAKING_REMOVE")]
    public async Task<IActionResult> Remove(StockTakingRemoveDto dto)
    {
        try
        {
            await _service.RemoveAsync(dto);
            return Ok(new { message = "Tag ditandai remove" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("manual-add")]
    [AuthorizePermissionHybrid("STOCK_TAKING_MANUAL")]
    public async Task<IActionResult> ManualAdd(StockTakingManualAddDto dto)
    {
        try
        {
            await _service.ManualAddAsync(dto);
            return Ok(new { message = "Manual add dicatat" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("finalize")]
    [AuthorizePermissionHybrid("STOCK_TAKING_FINALIZE")]
    public async Task<IActionResult> Finalize(StockTakingFinalizeDto dto)
    {
        try
        {
            var user = User.Identity?.Name ?? "system";
            await _service.FinalizeAsync(dto, user);
            return Ok(new { message = "Stock Taking selesai" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("apply-adjustment")]
    public async Task<IActionResult> ApplyAdjustment([FromBody] StockTakingFinalizeDto dto)
    {
        try
        {
            var user = User.Identity?.Name ?? "system";
            await _service.ApplyAdjustmentAsync(dto.SttId, user);
            return Ok(new { message = "Adjustment berhasil diterapkan" });
        }
        catch (Exception ex)
        {
            DailyFileLogger.Error("ApplyAdjustment error", ex);
            return StatusCode(500, new { message = ex.Message });
        }
    }

}