using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
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
        var deletedBy = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.DeleteAsync(id, deletedBy);
        return Ok(new { message = "Reader berhasil dihapus" });
    }

    [HttpGet("setting")]
    public async Task<IActionResult> GetSetting(string readerId)
    {
        if (string.IsNullOrWhiteSpace(readerId))
        {
            return BadRequest("Reader is required");
        }

        var data = await _service.GetReaderSettingAsync(readerId);

        return Ok(data);
    }

    [HttpPost("setting")]
    public async Task<IActionResult> SaveSetting(ReaderSettingDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ReaderId))
        {
            return BadRequest("Reader is required");
        }
        var user = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.SaveReaderSettingAsync(dto, user);

        return Ok("Reader setting saved");
    }

}