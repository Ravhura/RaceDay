using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for every controller: who is logged in, and what to do when an API call fails.</summary>
public abstract class AppController : Controller
{
    protected int UserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;
    protected string Role => HttpContext.Session.GetString(SessionKeys.Role) ?? "";
    protected string FullName => HttpContext.Session.GetString(SessionKeys.FullName) ?? "";
    protected bool IsLoggedIn => UserId != 0;

    /// <summary>
    /// Used when an API call fails. An expired session goes back to the login page,
    /// anything else shows a friendly error page.
    /// </summary>
    protected IActionResult ApiFailed<T>(ApiResult<T> result)
    {
        if (result.StatusCode == 401)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Login", "Account");
        }
        return View("ApiError", result.Error);
    }
}