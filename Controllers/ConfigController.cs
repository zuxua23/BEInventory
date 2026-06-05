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

    [HttpGet("printer")]
    public IActionResult GetPrinter()
    {
        return Ok(new
        {
            printerName = _configService.GetPrinterName()
        });
    }

    [HttpPut("printer")]
    public IActionResult UpdatePrinter([FromBody] UpdatePrinterDto dto)
    {
        _configService.UpdatePrinterName(dto.PrinterName);

        return Ok(new
        {
            message = "Printer name updated successfully."
        });
    }

    [HttpGet("advanced-setting")]
    public IActionResult GetAdvancedSetting()
    {
        try
        {
            var data = _configService.GetRfidAdvancedSetting();
            return Ok(data);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("advanced-setting")]
    public IActionResult UpdateAdvancedSetting([FromBody] RfidAdvancedSettingDto dto)
    {
        try
        {
            _configService.UpdateRfidAdvancedSetting(dto);

            return Ok(new
            {
                message = "RFID advanced setting updated successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}