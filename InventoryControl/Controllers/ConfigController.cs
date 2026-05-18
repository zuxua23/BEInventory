using InventoryControl.DTO;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[ApiController]
[Route("")]
public class ConfigController : Controller
{
    private readonly ConfigService _configService;
    private readonly AppRestartService _restartService;

    public ConfigController(ConfigService configService, AppRestartService restartService)
    {
        _configService = configService;
        _restartService = restartService;
    }

    [HttpPost("update-connection")]
    public IActionResult UpdateConnection([FromBody] UpdateConnectionDto dto)
    {
        try
        {
            _configService.UpdateConnection(dto);

            _restartService.Restart();

            return Ok(new { message = "Connection updated. Restarting app..." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}