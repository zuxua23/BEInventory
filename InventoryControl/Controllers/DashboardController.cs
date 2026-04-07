using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

public class DashboardController : Controller
{
        [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        ViewData["pages"] = "dashboard";
        return View();
    }
}
