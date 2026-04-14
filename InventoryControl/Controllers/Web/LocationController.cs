using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class LocationController : Controller
{
    [HttpGet("/location")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "location";
        ViewData["parent"] = "master";
        return View();
    }

    [HttpGet("/location/detail")]
    public IActionResult Detail(string id)
    {
        ViewData["id"] = id;
        return View();
    }
}

