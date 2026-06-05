using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class StockTakingController : Controller
{
    [HttpGet("/stockTaking")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "stockTaking";
        ViewData["parent"] = "";
        return View();
    }

}
