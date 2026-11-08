using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Put [RequireRole("Organiser")] on a controller to protect it.
/// Not logged in: redirected to the login page. Wrong role: redirected to "access denied".
/// </summary>
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string _role;

    public RequireRoleAttribute(string role)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId == null || string.IsNullOrEmpty(role))
        {
            if (context.Controller is Controller controller)
            {
                controller.TempData["Error"] = "Please log in to continue.";
            }
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (role != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}