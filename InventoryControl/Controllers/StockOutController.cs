using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Helpers;
using InventoryControl.Service.Implementations;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static InventoryControl.Service.Implementations.StockOutService;

namespace InventoryControl.Controllers;
[ApiController]
[Route("stockout")]
public class StockOutController : ControllerBase
{
    private readonly IStockOutService _service;
    private readonly ImpinjReaderService _readerService;
    private readonly AppDBContext _db;

    public StockOutController(IStockOutService service, ImpinjReaderService readerService, AppDBContext db)
    {
        _service = service;
        _readerService = readerService;
        _db = db;
    }

    [HttpPost("finalize")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public async Task<IActionResult> Finalize(StockOutDto dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.StockOutAsync(dto, user);

        return Ok("Stock Out finalized");
    }

    [HttpPost("scan")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public async Task<IActionResult> Scan(StockOutResponseDto dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.ScanStockOutAsync(dto, user);

        return Ok("Scanned");
    }

    [HttpPost("start")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public async Task<IActionResult> StartAsync(StartScanDto dto)
    {
        RfidSession.Set(dto.ReaderId, dto.DoId);
        Console.WriteLine("IP" + dto.IpAddress);

        await _readerService.StartReader(dto.ReaderId, dto.IpAddress);

        return Ok("Reader started");
    }

    [HttpPost("stop")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public IActionResult Stop(string readerId)
    {
        _readerService.StopReader(readerId);
        return Ok("Reader stopped");
    }


    [HttpGet("items")]
    [AuthorizePermissionHybrid("ITEM_GET")]
    public async Task<IActionResult> GetItems(string doId)
    {
        Console.WriteLine("=============================="+doId);

        var data = await _service.GetItemsAsync(doId);
        return Ok(data);
    }

    [HttpGet("progress")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public async Task<IActionResult> GetProgress(string doId)
    {
        var data = await _service.GetProgressAsync(doId);
        return Ok(data);
    }

    [HttpGet("tags")]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetTags(string doId)
    {
        var data = await _service.GetTagsAsync(doId);
        return Ok(data);
    }
}
