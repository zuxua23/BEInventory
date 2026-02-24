using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryControl.DTO;
using InventoryControl.Services.Interfaces;
using System.Security.Claims;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
[Route("user")]
public class UserApiController : ControllerBase
{
    private readonly IUserService _service;

    public UserApiController(IUserService service)
    {
        _service = service;
    }

    // READ
    [Authorize(Policy = "USER_VIEW")]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    // READ BY ID
    [Authorize(Policy = "USER_VIEW")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }

    // CREATE
    [Authorize(Policy = "USER_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create(UserDto dto)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "User berhasil dibuat" });
    }

    // UPDATE
    [Authorize(Policy = "USER_UPDATE")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateUserDto dto)
    {
        var updatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "User berhasil diperbarui" });
    }

    // DELETE
    [Authorize(Policy = "USER_DELETE")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "User berhasil dihapus" });
    }
}
