using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Implementations;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[InventoryLock]
[ApiController]
[Route("api/reader")]
public class ReaderApiController : ControllerBase
{
    private readonly IReaderService _service;

    public ReaderApiController(IReaderService service)
    {
        _service = service;
    }

    [HttpGet]
    [AuthorizePermissionHybrid("READER_GET")]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    [AuthorizePermissionHybrid("READER_GET")]
    public async Task<IActionResult> GetById(string id)
    {
        var reader = await _service.GetByIdAsync(id);
        if (reader == null)
            return NotFound(new { message = "Reader tidak ditemukan" });
        return Ok(reader);
    }

    [HttpPost]
    [AuthorizePermissionHybrid("READER_CREATE")]
    public async Task<IActionResult> Create(ReaderDto dto)
    {
        var createdBy = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Reader berhasil dibuat" });
    }


    [HttpPut("{id}")]
    [AuthorizePermissionHybrid("READER_UPDATE")]
    public async Task<IActionResult> Update(string id, ReaderDto dto)
    {
        var updatedBy = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.UpdateAsync(id, dto, updatedBy);

        return Ok(new { message = "Reader berhasil diupdate" });
    }

    [HttpDelete("{id}")]
    [AuthorizePermissionHybrid("READER_DELETE")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Reader berhasil dihapus" });
    }
}