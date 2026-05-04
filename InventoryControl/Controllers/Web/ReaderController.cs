using InventoryControl.Utility;
using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class ReaderController : Controller
{
    [HttpGet("/reader")]
    [AuthorizePermissionHybrid("READER_GET")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "reader";
        ViewData["parent"] = "master";
        return View();
    }

    [HttpGet("/reader/detail")]
    [AuthorizePermissionHybrid("READER_GET")]
    public IActionResult Detail(string id)
    {
        ViewData["id"] = id;
        return View();
    }
}

