namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/item")]
public class ItemApiController : ControllerBase
{
    private readonly IItemService _service;

    public ItemApiController(IItemService service)
    {
        _service = service;
    }

    //public IActionResult Index()
    //{
    //    ViewData["pages"] = "item";
    //    return View();
    //}

    [HttpGet]
    [AuthorizePermissionHybrid("ITEM_GET")]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    [AuthorizePermissionHybrid("ITEM_GET")]

    public async Task<IActionResult> GetById(string id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }

    [HttpPost]
    [AuthorizePermissionHybrid("ITEM_CREATE")]
    public async Task<IActionResult> Create(ItemDto dto)
    {
        var createdBy = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Item berhasil dibuat" });
    }

    [HttpPut("{id}")]
    [AuthorizePermissionHybrid("ITEM_UPDATE")]
    public async Task<IActionResult> Update(string id, ItemUpdateDto dto)
    {
        var updatedBy = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "Item berhasil diperbarui" });
    }

   
    [HttpDelete("{id}")]
    [AuthorizePermissionHybrid("ITEM_DELETE")]

    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Item berhasil dihapus" });
    }


}