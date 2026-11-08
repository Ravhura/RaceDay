using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    public IActionResult Index()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Account");
        return View();
    }

    public IActionResult Error() => View();
}