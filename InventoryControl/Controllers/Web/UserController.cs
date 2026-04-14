namespace InventoryControl.Controllers.Web;

using Microsoft.AspNetCore.Mvc;


public class UserController : Controller
{
    [HttpGet("/user")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");

        if (user == null)
            return Redirect("/");

        ViewData["pages"] = "user";
        ViewData["parent"] = "master";

        return View();
    }
    [HttpGet("/user/detail")]
    public IActionResult Detail(string id)
    {
        ViewData["id"] = id;
        return View();
    }
}