using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class PickingListController : Controller
{
    [HttpGet("/pickinglist")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "pickinglist";
        ViewData["parent"] = "master";
        return View();
    }

    [HttpGet("/pickinglist/detail")]
    public IActionResult Detail(string id)
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "pickinglist";
        ViewData["parent"] = "master";
        ViewData["id"] = id;
        return View();
    }
}

