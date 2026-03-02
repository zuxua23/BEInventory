namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("item")]
public class ItemApiController : ControllerBase
{
    private readonly IItemService _service;

    public ItemApiController(IItemService service)
    {
        _service = service;
    }

    // READ ALL
    [Authorize(Policy = "MASTER_ITEM_VIEW")]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    // READ BY ID
    [Authorize(Policy = "MASTER_ITEM_VIEW")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }

    // CREATE
    [Authorize(Policy = "MASTER_ITEM_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create(ItemDto dto)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Item berhasil dibuat" });
    }

    // UPDATE
    [Authorize(Policy = "MASTER_ITEM_UPDATE")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateItemDto dto)
    {
        var updatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "Item berhasil diperbarui" });
    }

    // DELETE
    [Authorize(Policy = "MASTER_ITEM_DELETE")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Item berhasil dihapus" });
    }
}