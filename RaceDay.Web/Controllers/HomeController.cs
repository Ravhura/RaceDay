using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    public IActionResult Index()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Account");
        if (Role == Roles.Organiser) return RedirectToAction("Dashboard", "Organiser");
        return View();
    }

    public IActionResult Error() => View();
}