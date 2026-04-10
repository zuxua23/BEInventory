using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

//[ApiController]
//[Route("stockin")]    
public class StockInController : ControllerBase
{
    private readonly IStockInService _service;

    public StockInController(IStockInService service)
    {
        _service = service;
    }

    //[HttpPost]
    //public async Task<IActionResult> StockIn(StockInDto dto)
    //{
    //    var user = User.Identity?.Name ?? "system";

    //    await _service.StockInAsync(dto, user);

    //    return Ok(new { message = "Stock In berhasil" });
    //}

    [HttpPost]
    public async Task<IActionResult> StockIn([FromBody] StockInDto dto) // Tambahkan [FromBody]
    {
        var user = User.Identity?.Name ?? "system";
        await _service.StockInAsync(dto, user);
        return Ok(new { message = "Stock In berhasil" });
    }

}