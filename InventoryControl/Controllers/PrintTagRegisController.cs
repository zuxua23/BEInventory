using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Implementations;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;
public class PrintTagRegisController: ControllerBase
{
    private readonly IPrintTagRegisService _service;

    public PrintTagRegisController(IPrintTagRegisService service)
    {
        _service = service;
    }

    [AuthorizePermissionHybrid("TAG_PRINT")]
    public async Task<IActionResult> Print([FromBody] List<PrintTagDto> dto)
    {
        if (dto == null || !dto.Any())
            return BadRequest("Data tidak boleh kosong");

        var user = User.Identity?.Name ?? "system";

        var batch = await _service.PrintBulkAsync(dto, user);

        return Ok(new
        {
            message = "Print berhasil",
            batchNo = batch
        });
    }

    [AuthorizePermissionHybrid("TAG_REGISTER")]
    public async Task<IActionResult> Register([FromBody] TagRegistrationDto dto)
    {
        var user = User.Identity?.Name ?? "system";

        await _service.RegisterAsync(dto, user);

        return Ok(new { message = "Tag successfully registered" });
    }

    [AuthorizePermissionHybrid("TAG_HISTORY_GET")]
    public async Task<IActionResult> GetPrintHistory()
    {
        var data = await _service.GetAvailableTagsAsync();
        return Ok(data);
    }
}
