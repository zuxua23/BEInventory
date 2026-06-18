using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class DashboardController : Controller
{
        [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        ViewData["pages"] = "dashboard";
        return View();
    }
}
