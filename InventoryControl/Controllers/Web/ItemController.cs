using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;


public class ItemController : Controller
{
    [HttpGet("/item")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "item";
        ViewData["parent"] = "master";
        return View();
    }

    [HttpGet("/item/detail")]
    public IActionResult Detail(string id)
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "item";
        ViewData["parent"] = "master";
        ViewData["id"] = id;
        return View();
    }
}
