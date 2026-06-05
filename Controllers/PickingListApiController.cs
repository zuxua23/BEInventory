using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventoryControl.Controllers;

[InventoryLock]
[ApiController]
[Route("api/pickinglist")]
public class PickingListApiController : ControllerBase
{
    private readonly IPickingListService _service;

    public PickingListApiController(IPickingListService service)
    {
        _service = service;
    }

    [HttpGet]
    [AuthorizePermissionHybrid("PICKINGLIST_GET")]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("count-do")]
    public async Task<IActionResult> GetDOStatusCount()
    {
        var statusCount = await _service.GetDOStatusCountAsync();
        return Ok(statusCount);
    }

    [HttpGet("{itemId}/available-stock-edit")]
    public async Task<IActionResult> GetAvailableStockForEdit(
    string itemId,
    [FromQuery] string? doId)
    {
        var stock = await _service.GetAvailableStockForEditAsync(itemId, doId);
        return Ok(stock);
    }


    [HttpGet("{id}")]
    [AuthorizePermissionHybrid("PICKINGLIST_GET")]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }

    [HttpPost]
    [AuthorizePermissionHybrid("PICKINGLIST_CREATE")]
    public async Task<IActionResult> Create([FromBody] PickingListDTO request)
    {
        if (request.Details == null || !request.Details.Any())
            return BadRequest("Detail DO tidak boleh kosong");

        var createdBy = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.CreateAsync(request, createdBy);

        return Ok(new { message = "DO berhasil dibuat" });
    }

    [HttpPut("{id}")]
    [AuthorizePermissionHybrid("PICKINGLIST_UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] PickingListDTO dto)
    {
        var updatedBy = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "DO berhasil diupdate" });
    }
    [HttpDelete("{id}")]
    [AuthorizePermissionHybrid("PICKINGLIST_DELETE")]
    public async Task<IActionResult> Delete(string id)
    {
        var deletedBy = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.DeleteAsync(id, deletedBy);
        return Ok(new { message = "DO berhasil dihapus" });
    }

}