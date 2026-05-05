namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Mvc;

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
        try
        {
            await _service.ScanAsync(dto);
            return Ok(new { message = "Tag discan" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("scan/bulk")]
    public async Task<IActionResult> BulkScan(StockTakingBulkScanDto dto)
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