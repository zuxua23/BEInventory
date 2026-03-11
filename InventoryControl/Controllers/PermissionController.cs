namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("permission")]
public class PermissionApiController : ControllerBase
{
    private readonly IPermissionService _service;

    public PermissionApiController(IPermissionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(PermissionDto dto)
    {
        var createdBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Permission berhasil dibuat" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, PermissionUpdateDto dto)
    {
        var updatedBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "Permission berhasil diperbarui" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Permission berhasil dihapus" });
    }
}