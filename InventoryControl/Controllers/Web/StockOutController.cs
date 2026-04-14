using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class StockOutController : Controller
{
    [HttpGet("/stockOut")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "stockOut";
        return View();
    }
}