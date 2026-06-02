using InventoryControl.Database;
using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Implementations;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static InventoryControl.Service.Implementations.StockOutService;

namespace InventoryControl.Controllers;

[InventoryLock]
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
        var user = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.StockOutAsync(dto, user);

        return Ok("Stock Out finalized");
    }

    [HttpPost("confirm")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public async Task<IActionResult> ConfirmSession([FromQuery] string doId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(doId))
            {
                return BadRequest(new
                {
                    type = "warning",
                    message = "DO is required."
                });
            }

            var user = HttpContext.Session.GetString("UserId") ?? "system";

            await _service.ConfirmStockOutAsync(doId, user);

            return Ok("Stock Out confirmed");
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                type = "error",
                message = ex.Message
            });
        }
    }

    [HttpPost("scan")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public async Task<IActionResult> Scan(StockOutResponseDto dto)
    {
        var user = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.ScanStockOutAsync(dto, user);

        return Ok("Scanned");
    }

    [HttpPost("start")]
    [AuthorizePermissionHybrid("STOCK_OUT")]
    public async Task<IActionResult> StartAsync(StartScanDto dto)
    {
        RfidSession.Set(dto.ReaderId, dto.DoId);
        Console.WriteLine($"[RFID START] ReaderId={dto.ReaderId}, DO={dto.DoId}, IP={dto.IpAddress}");

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

    [HttpGet("invalid-scan")]
    public IActionResult GetInvalidScan()
    {
        if (StockOutService.LastInvalidTag == null)
        {
            return Ok(null);
        }

        var result = new
        {
            tagId = StockOutService.LastInvalidTag
        };

        StockOutService.LastInvalidTag = null;

        return Ok(result);
    }
}
