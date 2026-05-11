using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;
[InventoryLock]
//[ApiController]
//[Route("stockin")]    
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
        var user = User.Identity?.Name ?? "system";
        await _service.StockInAsync(dto, user);
        return Ok(new { message = "Stock In berhasil" });
    }

}