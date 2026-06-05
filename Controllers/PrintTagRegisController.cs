using InventoryControl.DTO;
using InventoryControl.Entity;
using InventoryControl.Service.Implementations;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[InventoryLock]
[ApiController]
[Route("api/tag")]
public class PrintTagRegisController: ControllerBase
{
    private readonly IPrintTagRegisService _service;

    public PrintTagRegisController(IPrintTagRegisService service)
    {
        _service = service;
    }

    [HttpGet]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllAsync();
        return Ok(data);
    }

    [HttpGet("tag-regis")]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetAllTagRegis()
    {
        var data = await _service.GetAllTagRegisAsync();
        return Ok(data);
    }

    [HttpGet("status-count")]
    public async Task<IActionResult> GetTagStatusCount()
    {
        var data = await _service.GetTagStatusCountAsync();
        return Ok(data);
    }

    [HttpPost("print")]
    [AuthorizePermissionHybrid("TAG_PRINT")]
    public async Task<IActionResult> Print([FromBody] List<PrintTagDto> dto)
    {
        if (dto == null || !dto.Any())
            return BadRequest("Data tidak boleh kosong");

        var user = HttpContext.Session.GetString("UserId") ?? "system";


        var batch = await _service.PrintBulkAsync(dto, user);

        return Ok(new
        {
            message = "Print berhasil",
            batchNo = batch
        });
    }

    [HttpPost("register")]
    [AuthorizePermissionHybrid("TAG_REGISTER")]
    public async Task<IActionResult> Register(TagRegistrationDto dto)
    {
        var user = HttpContext.Session.GetString("UserId") ?? "system";


        await _service.RegisterAsync(dto, user);

        return Ok(new { message = "Tag berhasil di-standby-kan" });
    }

    [HttpGet("history")]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetPrintHistory()
    {
        var data = await _service.GetAvailableTagsAsync();
        return Ok(data);
    }
    [HttpGet("stock")]
    public async Task<IActionResult> GetStock()
    {
        var data = await _service.GetStockPerItemAsync();
        return Ok(data);
    }
    [HttpGet("qr")]
    [AuthorizePermissionHybrid("TAG_GET")]
    public async Task<IActionResult> GetStockQR(string tagId)
    {
        var data = await _service.GetByQRAsync(tagId);
        return Ok(data);
    }



}
