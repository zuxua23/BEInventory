using InventoryControl.DTO;
using InventoryControl.Utility;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/user")]
public class UserApiController : ControllerBase
{
    private readonly IUserService _service;

    public UserApiController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    [AuthorizePermissionHybrid("USER_GET")]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    [AuthorizePermissionHybrid("USER_GET")]
    public async Task<IActionResult> GetById(string id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound();

        return Ok(data);
    }


    [HttpPost]
    [AuthorizePermissionHybrid("USER_CREATE")]
    public async Task<IActionResult> Create(UserDto dto)
    {
        var createdBy = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "User berhasil dibuat" });
    }

    [HttpPut("{id}")]
    [AuthorizePermissionHybrid("USER_UPDATE")]
    public async Task<IActionResult> Update(string id, UpdateUserDto dto)
    {
        var updatedBy = HttpContext.Session.GetString("UserId") ?? "system";
        await _service.UpdateAsync(id, dto, updatedBy);
        return Ok(new { message = "User berhasil diperbarui" });
    }

    [HttpPost("update-roles")]
    [AuthorizePermissionHybrid("USER_UPDATE")]
    public async Task<IActionResult> UpdateRoles([FromBody] UpdateUserRoleDto dto)
    {
        try
        {
            var user = HttpContext.User.Identity?.Name ?? "system";

            await _service.UpdateUserRolesAsync(dto, user);

            return Ok(new { message = "Role berhasil diupdate" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPut("{id}/password")]
    [AuthorizePermissionHybrid("USER_UPDATE")]
    public async Task<IActionResult> UpdatePassword(string id, UpdatePasswordDto dto)
    {
        var user = HttpContext.Session.GetString("UserId") ?? "system";

        await _service.UpdatePasswordAsync(id, dto, user);

        return Ok(new { message = "Password updated successfully" });
    }

    [HttpDelete("{id}")]
    [AuthorizePermissionHybrid("USER_DELETE")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "User berhasil dihapus" });
    }
}
