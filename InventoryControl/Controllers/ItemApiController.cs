namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("item")]
public class ItemApiController : ControllerBase
{
    private readonly IItemService _service;

    public ItemApiController(IItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ItemDto dto)
    {
        var createdBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";

        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Item berhasil dibuat" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, ItemDto dto)
    {
        var updatedBy = Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";

        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "Item berhasil diperbarui" });
    }

   
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Item berhasil dihapus" });
    }
}