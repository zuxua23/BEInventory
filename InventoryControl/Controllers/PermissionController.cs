namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("permission")]
public class PermissionApiController : ControllerBase
{
    private readonly IPermissionService _service;

    public PermissionApiController(IPermissionService service)
    {
        _service = service;
    }

    [Authorize(Policy = "MASTER_PERMISSION_VIEW")]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [Authorize(Policy = "MASTER_PERMISSION_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create(PermissionDto dto)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Permission berhasil dibuat" });
    }

    [Authorize(Policy = "MASTER_PERMISSION_UPDATE")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, PermissionDto dto)
    {
        var updatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "Permission berhasil diperbarui" });
    }

    [Authorize(Policy = "MASTER_PERMISSION_DELETE")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Permission berhasil dihapus" });
    }
}