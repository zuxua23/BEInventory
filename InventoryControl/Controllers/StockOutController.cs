using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[Authorize]
[ApiController]
[Route("stock/out")]
public class StockOutController : ControllerBase
{
    private readonly IStockOutService _service;
    private readonly ReaderScan _readerScan;
    public StockOutController(IStockOutService service,ReaderScan scan)
    {
        _service = service;
        _readerScan = scan;
    }

    [Authorize(Policy = "TRANS_STOCK_OUT")]
    [HttpPost]
    public async Task<IActionResult> Finalize(StockOutDto dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.StockOutAsync(dto, user);

        return Ok(new { message = "Stock Out berhasil difinalisasi" });
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartScan(string readerId, string doId)
    {
        var user = User.Identity?.Name ?? "system";

        await _readerScan.StartScanAsync(readerId, doId, user);

        return Ok("Reader scanning started");
    }

    [HttpPost("stop")]
    public IActionResult StopScan()
    {
        _readerScan.StopScan();
        return Ok("Reader scanning stopped");
    }
}
