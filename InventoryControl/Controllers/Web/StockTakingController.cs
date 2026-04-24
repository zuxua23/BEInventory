using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class StockTakingController : Controller
{
    [HttpGet("/stock-taking")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "stock-taking";
        ViewData["parent"] = "";
        return View();
    }

}
