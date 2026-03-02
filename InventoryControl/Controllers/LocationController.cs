using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[Authorize]
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
    public async Task<IActionResult> Create(Location dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.CreateAsync(dto, user);
        return Ok();
    }

    [Authorize(Policy = "MASTER_LOCATION_UPDATE")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Location dto)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.UpdateAsync(id, dto, user);
        return Ok();
    }

    [Authorize(Policy = "MASTER_LOCATION_DELETE")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }
}