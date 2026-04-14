using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers.Web;

public class PrintTagRegisController : Controller
{
    [HttpGet("/printtag")]
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString("UserId");
        if (user == null)
            return Redirect("/");
        ViewData["pages"] = "printtag";
        ViewData["parent"] = "";
        return View();
    }   
}
