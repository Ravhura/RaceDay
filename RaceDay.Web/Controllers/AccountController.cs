using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in, log out. Everything goes through the Part 2 API.</summary>
public class AccountController : AppController
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (IsLoggedIn) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        HttpContext.Session.Clear();   // start from a clean session
        var result = await _api.LoginAsync(model.Role, model.Email.Trim(), model.Password);

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        // Remember who is logged in. Only the id, name and role are stored, never the password.
        HttpContext.Session.SetInt32(SessionKeys.UserId, result.Data.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, result.Data.Role);
        HttpContext.Session.SetString(SessionKeys.FullName, result.Data.FullName);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (IsLoggedIn) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.RegisterAsync(model.Role, new
        {
            fullName = model.FullName.Trim(),
            email = model.Email.Trim(),
            password = model.Password
        });

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction("Login");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (IsLoggedIn)
        {
            await _api.LogoutAsync();      // ends the API session
        }
        HttpContext.Session.Clear();       // ends this app's session

        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();
}