using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[ApiController]
[Route("stockout")]
public class StockOutController : ControllerBase
{
    private readonly IStockOutService _service;
    private readonly ReaderScan _readerScan;
    public StockOutController(IStockOutService service,ReaderScan scan)
    {
        _service = service;
        _readerScan = scan;
    }

    [HttpPost]
    public async Task<IActionResult> Finalize(StockOutDto dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.StockOutAsync(dto, user);

        return Ok(new { message = "Stock Out berhasil difinalisasi" });
    }
    [HttpPost]
    public async Task<IActionResult> Scan(StockOutResponseDto dto)
    {
        var user = User.Identity?.Name ?? "system";
         await _service.ScanStockOutAsync(dto, user);
        return Ok(new { message = "Stock Out berhasil dibuat" });
    }


    [HttpPost("start")]
    public async Task<IActionResult> StartScan(StockOutResponseDto dto, string doId)
    {

        await _readerScan.StartScanAsync(dto, doId);

        return Ok("Reader scanning started");
    }

    [HttpPost("stop")]
    public IActionResult StopScan()
    {
        _readerScan.StopScan();
        return Ok("Reader scanning stopped");
    }
}
