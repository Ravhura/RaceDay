using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Shared helpers for every controller: who is logged in (read from the server-side session),
/// the 401/403 checks, and turning a service result into an HTTP response.
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>The logged-in user's id from the session, or 0 if nobody is logged in.</summary>
    protected int UserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    /// <summary>The logged-in user's role from the session, or "" if nobody is logged in.</summary>
    protected string Role => HttpContext.Session.GetString(SessionKeys.Role) ?? "";

    /// <summary>Returns 401 if nobody is logged in. Returns null if the user is logged in (carry on).</summary>
    protected ActionResult? RequireLogin()
    {
        if (UserId == 0)
        {
            return Unauthorized(new { message = "You must log in to access this resource." });
        }
        return null;
    }

    /// <summary>401 if not logged in, 403 if logged in with the wrong role, null if the role is correct.</summary>
    protected ActionResult? RequireRole(string role)
    {
        if (UserId == 0)
        {
            return Unauthorized(new { message = "You must log in to access this resource." });
        }
        if (Role != role)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Your role does not allow access to this resource." });
        }
        return null;
    }

    /// <summary>The id in the URL must be the logged-in user's own id, otherwise 403.</summary>
    protected ActionResult? RequireSelf(int id)
    {
        if (UserId != id)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only access your own data." });
        }
        return null;
    }

    /// <summary>Turns a service result into the matching HTTP response.</summary>
    protected ActionResult Respond<T>(ServiceResult<T> result)
    {
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }
        if (result.StatusCode == 204)
        {
            return NoContent();
        }
        return StatusCode(result.StatusCode, result.Data);
    }
}