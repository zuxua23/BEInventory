using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryControl.DTO;
using InventoryControl.Services.Interfaces;
using StackExchange.Redis;
using System.Security.Claims;

namespace InventoryControl.Controllers.Api;

[ApiController]
[Route("auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthApiController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        var token = await _authService.LoginAsync(dto);

        return Ok(new
        {
            success = true,
            token
        });
    }
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var permissions = User.FindAll("permission")
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        return Ok(new
        {
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            username = User.Identity?.Name,
            roles,
            permissions
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { message = "User tidak valid" });
        }

        await _authService.LogoutAsync(userId);

        return Ok(new { message = "Logout berhasil" });
    }
}
    

