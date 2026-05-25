using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;
[InventoryLock]
[ApiController]
[Route("stockin")]
public class StockInController : ControllerBase
{
    private readonly IStockInService _service;

    public StockInController(IStockInService service)
    {
        _service = service;
    }

    [HttpPost]
    [AuthorizePermissionHybrid("STOCK_IN")]
    public async Task<IActionResult> StockIn([FromBody] StockInDto dto) 
    {
       try
        {
            var user = User.Identity?.Name ?? "system";
            await _service.StockInAsync(dto, user);
            return Ok(new { message = "Stock In successful" });
        }
        catch(Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetTagByCode(string code, string scannerType)
    {
        try
        {
            var result = await _service.GetTagByCodeAsync(code, scannerType);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}