using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventoryControl.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("location")]
public class LocationApiController : ControllerBase
{
    private readonly ILocationService _service;

    public LocationApiController(ILocationService service)
    {
        _service = service;
    }

    [Authorize(Policy = "MASTER_LOCATION_VIEW")]
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _service.GetAllAsync());

    [Authorize(Policy = "MASTER_LOCATION_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create(LocationDTO dto)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Location berhasil dibuat" });
    }

    [Authorize(Policy = "MASTER_LOCATION_UPDATE")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, LocationDTO dto)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        await _service.UpdateAsync(id, dto, createdBy);
        return Ok(new { message = "Location berhasil diubah" });
    }

    [Authorize(Policy = "MASTER_LOCATION_DELETE")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Location berhasil dihapus" });
    }
}