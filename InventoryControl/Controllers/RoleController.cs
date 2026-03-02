namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("role")]
public class RoleApiController : ControllerBase
{
    private readonly IRoleService _service;

    public RoleApiController(IRoleService service)
    {
        _service = service;
    }

    [Authorize(Policy = "MASTER_ROLE_VIEW")]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAllAsync());
    }

    [Authorize(Policy = "MASTER_ROLE_CREATE")]
    [HttpPost]
    public async Task<IActionResult> Create(RoleDto dto)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
        await _service.CreateAsync(dto, createdBy);
        return Ok(new { message = "Role berhasil dibuat" });
    }

    [Authorize(Policy = "MASTER_ROLE_ASSIGN_PERMISSION")]
    [HttpPost("{roleId}/permissions")]
    public async Task<IActionResult> AssignPermission(string roleId, AssignPermissionDto dto)
    {
        await _service.AssignPermissionsAsync(roleId, dto.PermissionIds);
        return Ok(new { message = "Permission berhasil ditambahkan ke role" });
    }

    [Authorize(Policy = "MASTER_USER_ASSIGN_ROLE")]
    [HttpPost("user/{userId}")]
    public async Task<IActionResult> AssignRoleToUser(string userId, AssignRoleDto dto)
    {
        await _service.AssignRolesToUserAsync(userId, dto.RoleIds);
        return Ok(new { message = "Role berhasil ditambahkan ke user" });
    }
}