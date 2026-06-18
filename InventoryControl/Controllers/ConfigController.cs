using InventoryControl.DTO;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[ApiController]
[Route("")]
public class ConfigController : Controller
{
    private readonly ConfigService _configService;

    public ConfigController(ConfigService configService)
    {
        _configService = configService;
    }

    [HttpPost("update-connection")]
    public IActionResult UpdateConnection([FromBody] UpdateConnectionDto dto)
    {
        try
        {
            _configService.UpdateConnection(dto);

            return Ok(new { message = "Connection updated. Restarting app..." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}