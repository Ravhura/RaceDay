using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and session management for both roles.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly SessionOptions _sessionOptions;

    public AuthController(AuthService auth, IOptions<SessionOptions> sessionOptions)
    {
        _auth = auth;
        _sessionOptions = sessionOptions.Value;
    }

    /// <summary>Creates a new Participant account.</summary>
    /// <remarks>The role is chosen by the route. Password: 8+ characters with a letter and a digit.</remarks>
    /// <response code="201">Account created; returns the new ParticipantId.</response>
    /// <response code="400">Invalid input (e.g. bad email or weak password).</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register/participant")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParticipantCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParticipantCreatedResponse>> RegisterParticipant(RegisterRequest request)
    {
        var participant = await _auth.RegisterParticipantAsync(request);
        var body = new ParticipantCreatedResponse(participant.ParticipantId, participant.FullName, participant.Email);
        return Created($"/api/participants/{participant.ParticipantId}", body);
    }

    /// <summary>Creates a new Organiser account.</summary>
    /// <response code="201">Account created; returns the new OrganiserId.</response>
    /// <response code="400">Invalid input (e.g. bad email or weak password).</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register/organiser")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrganiserCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganiserCreatedResponse>> RegisterOrganiser(RegisterRequest request)
    {
        var organiser = await _auth.RegisterOrganiserAsync(request);
        var body = new OrganiserCreatedResponse(organiser.OrganiserId, organiser.FullName, organiser.Email);
        return Created($"/api/organisers/{organiser.OrganiserId}", body);
    }

    /// <summary>Logs in a Participant and starts a server-side session.</summary>
    /// <remarks>The session cookie is set automatically. In Swagger UI, later requests send it for you.</remarks>
    /// <response code="200">Logged in; returns the user's id, name and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login/participant")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginParticipant(LoginRequest request)
    {
        var participant = await _auth.LoginParticipantAsync(request);
        StartSession(participant.ParticipantId, Roles.Participant);
        return Ok(new LoginResponse(participant.ParticipantId, participant.FullName, participant.Email, Roles.Participant));
    }

    /// <summary>Logs in an Organiser and starts a server-side session.</summary>
    /// <response code="200">Logged in; returns the user's id, name and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login/organiser")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> LoginOrganiser(LoginRequest request)
    {
        var organiser = await _auth.LoginOrganiserAsync(request);
        StartSession(organiser.OrganiserId, Roles.Organiser);
        return Ok(new LoginResponse(organiser.OrganiserId, organiser.FullName, organiser.Email, Roles.Organiser));
    }

    /// <summary>Renews the current session (keeps the user logged in).</summary>
    /// <remarks>
    /// RaceDay uses server-side sessions, not tokens, so there is no refresh token to exchange.
    /// This endpoint keeps the Part 1 route and resets the session's idle timeout.
    /// </remarks>
    /// <response code="200">Session renewed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("refresh-token")]
    [Authorize]
    [ProducesResponseType(typeof(SessionRenewedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<SessionRenewedResponse> RefreshSession()
    {
        // Reading the session in this request already resets its sliding idle timeout.
        var minutes = (int)_sessionOptions.IdleTimeout.TotalMinutes;
        return Ok(new SessionRenewedResponse(minutes, "Session renewed."));
    }

    /// <summary>Logs out the current user by clearing the server-side session.</summary>
    /// <response code="200">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<MessageResponse> Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new MessageResponse("Logged out."));
    }

    private void StartSession(int userId, string role)
    {
        HttpContext.Session.Clear(); // replace any existing session
        HttpContext.Session.SetInt32(SessionKeys.UserId, userId);
        HttpContext.Session.SetString(SessionKeys.Role, role);
    }
}