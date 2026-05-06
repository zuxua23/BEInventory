using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{

    private readonly ILogService _logService;

    public LogsController(
        ILogService logService
    )
    {
        _logService = logService;
    }

    [HttpGet("recent-activity")]
    public IActionResult GetRecentActivity()
    {
        var result =
            _logService.GetRecentActivities(5);

        return Ok(result);
    }
}