using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventoryControl.Controllers;

[InventoryLock]
[ApiController]
[Route("api/location")]
public class LocationApiController : ControllerBase
{
    private readonly ILocationService _service;

    public LocationApiController(ILocationService service)
    {
        _service = service;
    }

    [HttpGet]
    [AuthorizePermissionHybrid("LOCATION_GET")]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());    
    }

    [HttpGet("{id}")]
    [AuthorizePermissionHybrid("LOCATION_GET")]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }

    [HttpPost]
    [AuthorizePermissionHybrid("LOCATION_CREATE")]
    public async Task<IActionResult> Create(LocationDTO dto)
    {
        var createdBy = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Location berhasil dibuat" });
    }

    [HttpPut("{id}")]
    [AuthorizePermissionHybrid("LOCATION_UPDATE")]
    public async Task<IActionResult> Update(string id, LocationDTO dto)
    {
        var updatedBy = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "Location berhasil diubah" });
    }

    [HttpDelete("{id}")]
    [AuthorizePermissionHybrid("LOCATION_DELETE")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Location berhasil dihapus" });
    }
}