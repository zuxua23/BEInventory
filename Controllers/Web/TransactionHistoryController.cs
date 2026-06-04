using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;


public class TransactionHistoryController : Controller
{
    [HttpGet("/TransactionHistory")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");

        ViewData["pages"] = "TransactionHistory";
        ViewData["parent"] = "";
        return View();
    }

    [HttpGet("/TransactionHistory/detail")]
    public IActionResult Detail(string id)
    {
        var user = HttpContext.Session.GetString("UserId");

        if (user == null)
            return Redirect("/");

        ViewData["pages"] = "TransactionHistory";
        ViewData["parent"] = "";
        ViewData["id"] = id;
        return View();
    }
}
