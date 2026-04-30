using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

//[ApiController]
//[Route("stockpreparation")]
public class StockPreparationController : ControllerBase
{
    private readonly IStockPreparationService _service;

    public StockPreparationController(IStockPreparationService service)
    {
        _service = service;
    }

    [AuthorizePermissionHybrid("STOCK_PREPARATION")]
    public async Task<IActionResult> Prepare(StockPreparationRequestDto dto)
    {
        try
        {
            var user = User.Identity?.Name ?? "system";
            await _service.PrepareAsync(dto, user);
            return Ok(new { message = "Tag prepared successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AuthorizePermissionHybrid("STOCK_PREPARATION")]
    public async Task<IActionResult> PrepareBulk([FromBody] StockPreparationBulkRequestDto dto)
    {
        try
        {
            var user = User.Identity?.Name ?? "system";
            await _service.PrepareBulkAsync(dto, user);
            return Ok(new { message = $"Successfully prepared {dto.ScannedCodes.Count} tags" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [AuthorizePermissionHybrid("STOCK_PREPARATION")]

    public async Task<IActionResult> GetDoDrafts()
    {
        try
        {
            var result = await _service.GetDoDraftAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}