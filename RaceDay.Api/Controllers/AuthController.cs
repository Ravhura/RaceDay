using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;
using RaceDay.Api.Authentication;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and session management for both roles.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Creates a new Participant account.</summary>
    /// <remarks>The role is chosen by the route. Password: 8+ characters with a letter and a digit.</remarks>
    /// <response code="201">Account created; returns the new ParticipantId.</response>
    /// <response code="400">Invalid input (e.g. bad email or weak password).</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register/participant")]
    [ProducesResponseType(typeof(ParticipantCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RegisterParticipant(RegisterRequest request)
    {
        return Respond(await _auth.RegisterParticipantAsync(request));
    }

    /// <summary>Creates a new Organiser account.</summary>
    /// <response code="201">Account created; returns the new OrganiserId.</response>
    /// <response code="400">Invalid input (e.g. bad email or weak password).</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register/organiser")]
    [ProducesResponseType(typeof(OrganiserCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RegisterOrganiser(RegisterRequest request)
    {
        return Respond(await _auth.RegisterOrganiserAsync(request));
    }

    /// <summary>Logs in a Participant and starts a server-side session.</summary>
    /// <remarks>The session cookie is set automatically. In Swagger UI, later requests send it for you.</remarks>
    /// <response code="200">Logged in; returns the user's id, name and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login/participant")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> LoginParticipant(LoginRequest request)
    {
        var result = await _auth.LoginParticipantAsync(request);
        if (result.Success)
        {
            StartSession(result.Data!.UserId, Roles.Participant);
        }
        return Respond(result);
    }

    /// <summary>Logs in an Organiser and starts a server-side session.</summary>
    /// <response code="200">Logged in; returns the user's id, name and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login/organiser")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> LoginOrganiser(LoginRequest request)
    {
        var result = await _auth.LoginOrganiserAsync(request);
        if (result.Success)
        {
            StartSession(result.Data!.UserId, Roles.Organiser);
        }
        return Respond(result);
    }

    /// <summary>Renews the current session (keeps the user logged in).</summary>
    /// <remarks>
    /// RaceDay uses server-side sessions, not tokens, so there is no refresh token to exchange.
    /// This endpoint keeps the Part 1 route and resets the session's idle timeout.
    /// </remarks>
    /// <response code="200">Session renewed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult RefreshSession()
    {
        var check = RequireLogin();
        if (check != null) return check;

        // Reading the session in this request already resets its idle timeout.
        return Ok(new { message = "Session renewed." });
    }

    /// <summary>Logs out the current user by clearing the server-side session.</summary>
    /// <response code="200">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult Logout()
    {
        var check = RequireLogin();
        if (check != null) return check;

        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }

    // Saves the user's id and role in the server-side session.
    private void StartSession(int userId, string role)
    {
        HttpContext.Session.Clear(); // replace any existing session
        HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
        HttpContext.Session.SetString(SessionKeys.Role, role);
    }
}