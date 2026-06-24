using InventoryControl.DTO;
using InventoryControl.Services.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InventoryControl.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    public IActionResult Index()
    {
        ViewData["pages"] = "Auth";
        return View();
    }

    public IActionResult Ping()
    {
        return Ok(new { status = "ok", message = "Server is Connected" });
    }

    public async Task<IActionResult> Login(LoginDTO dto)
    {
        try
        {
            var result = await _authService.ValidateUserAsync(dto);

            HttpContext.Session.SetString("UserId", result.UserId.ToString());
            HttpContext.Session.SetString("Username", result.Username);
            HttpContext.Session.SetString("Fullname", result.Fullname);   
            HttpContext.Session.SetString("Roles", JsonSerializer.Serialize(result.Roles));
            HttpContext.Session.SetString("Permissions", JsonSerializer.Serialize(result.Permissions));
            HttpContext.Session.SetString("is_login", "OK");

            return Redirect("/dashboard");
        }
        catch (Exception)
        {
            TempData["LoginError"] = "Username atau password salah.";
            return RedirectToAction("Index");
        }
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> LoginHT([FromBody] LoginDTO dto)
    {
        try
        {
            var result = await _authService.ValidateUserAsync(dto);
            var token = await _authService.GenerateTokenAsync(result);

            DailyFileLogger.Audit(
                action: "LOGIN_ANDROID",
                entity: "USER",
                entityId: result.UserId,
                performedBy: result.Fullname,
                description: "User login via Android/HT device."
            );

            return Ok(new
            {
                token = token,
                token_type = "Bearer",
                user = result.Username,
                fullname = result.Fullname,    
                roles = result.Roles,
                permissions = result.Permissions
            });
        }
        catch (Exception e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    public IActionResult LogoutHT()
    {
        return Ok(new { message = "Logout success (client delete token)" });
    }
}