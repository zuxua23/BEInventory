using Microsoft.AspNetCore.Mvc;

namespace InventoryControl.Controllers;

public class HomeController : Controller
{

    public IActionResult Index()
    {
        Console.WriteLine("home");
        ViewData["pages"] = "home";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}