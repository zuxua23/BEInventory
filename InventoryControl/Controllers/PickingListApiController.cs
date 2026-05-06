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

        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

        await _service.CreateAsync(request, createdBy);

        return Ok(new { message = "DO berhasil dibuat" });
    }

    [HttpPut("{id}")]
    [AuthorizePermissionHybrid("PICKINGLIST_UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] PickingListDTO dto)
    {
        await _service.UpdateAsync(id, dto);
        return Ok(new { message = "DO berhasil diupdate" });
    }
    [HttpDelete("{id}")]
    [AuthorizePermissionHybrid("PICKINGLIST_DELETE")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "DO berhasil dihapus" });
    }

    //[HttpPut("{id}")]
    //[AuthorizePermissionHybrid("PICKINGLIST_UPDATE_STATUS")]
    //public async Task<IActionResult> UpdateStatus(string id, DOStatusUpdateDto dto)
    //{
    //    await _service.UpdateStatusAsync(id, dto.Status);
    //    return Ok(new { message = "Status DO berhasil diperbarui" });
    //}
}