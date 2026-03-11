using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Implementations;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;
[ApiController]
[Route("tag")]
public class PrintTagRegisController: ControllerBase
{
    private readonly IPrintTagRegisService _service;

    public PrintTagRegisController(IPrintTagRegisService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Print(PrintTagDto dto)
    {
        var user = User.Identity?.Name ?? "system";
        var batch = await _service.PrintAsync(dto, user);

        return Ok(new { message = "Print berhasil", batchNo = batch });
    }

    [HttpPost]
    public async Task<IActionResult> Register(TagRegistrationDto dto)
    {
        var user = User.Identity?.Name ?? "system";

        await _service.RegisterAsync(dto, user);

        return Ok(new { message = "Tag berhasil di-standby-kan" });
    }

    [HttpGet]
    public async Task<IActionResult> GetPrintHistory()
    {
        var data = await _service.GetAvailableTagsAsync();
        return Ok(data);
    }
}
