using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[Authorize]
[ApiController]
[Route("do")]
public class DOApiController : ControllerBase
{
    private readonly IDOService _service;

    public DOApiController(IDOService service)
    {
        _service = service;
    }

    [Authorize(Policy = "TRANS_DO_VIEW")]
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _service.GetAllAsync());

    [Authorize(Policy = "TRANS_DO_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DOCreateRequest request)
    {
        var user = User.Identity?.Name ?? "system";
        await _service.CreateAsync(request.DO, request.Details, user);
        return Ok();
    }

    [Authorize(Policy = "TRANS_DO_DELETE")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }
}