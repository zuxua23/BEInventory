using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventoryControl.Controllers;

[ApiController]
[Route("location")]
public class LocationApiController : ControllerBase
{
    private readonly ILocationService _service;

    public LocationApiController(ILocationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());    
    }

    [HttpPost]
    public async Task<IActionResult> Create(LocationDTO dto)
    {
        var createdBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";

        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Location berhasil dibuat" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, LocationDTO dto)
    {
        var updatedBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "Location berhasil diubah" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Location berhasil dihapus" });
    }
}