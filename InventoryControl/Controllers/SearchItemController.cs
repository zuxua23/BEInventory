using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace InventoryControl.Controllers;

[ApiController]
[Route("api/search-item")]
public class SearchItemController : ControllerBase
{
    private readonly ISearchItemService _service;

    public SearchItemController(ISearchItemService service)
    {
        _service = service;
    }   
    [HttpGet]
    [AuthorizePermissionHybrid("PERMISSION_GET")]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetAllItemsAsync();
        return Ok(data);
    }

    [HttpGet("{code}")]
    [AuthorizePermissionHybrid("PERMISSION_GET")]
    public async Task<IActionResult> GetDetail(string code)
    {
        var data = await _service.GetTagDetailAsync(code);
        if (data == null) return NotFound(new { message = "Tag not found or already deleted." });
        return Ok(data);
    }
}