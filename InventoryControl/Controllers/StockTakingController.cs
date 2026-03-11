namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("stocktaking")]
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
        var user = User.Identity?.Name ?? "system";
        var id = await _service.CreateAsync(dto, user);
        return Ok(new { StockTakingId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetStockData()
    {
        var data = await _service.GetStockDataAsync();
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Scan(StockTakingScanDto dto)
    {
        await _service.ScanAsync(dto);
        return Ok(new { message = "Tag discan" });
    }

    [HttpPost]
    public async Task<IActionResult> Remove(StockTakingRemoveDto dto)
    {
        await _service.RemoveAsync(dto);
        return Ok(new { message = "Tag ditandai remove" });
    }

    [HttpPost]
    public async Task<IActionResult> ManualAdd(StockTakingManualAddDto dto)
    {
        await _service.ManualAddAsync(dto);
        return Ok(new { message = "Manual add dicatat" });
    }

    [HttpPost]
    public async Task<IActionResult> Finalize(StockTakingFinalizeDto dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.FinalizeAsync(dto, user);
        return Ok(new { message = "Stock Taking selesai" });
    }
}