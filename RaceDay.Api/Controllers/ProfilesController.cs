using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>View and update your own profile. The {id} must be your own ID.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ProfilesController : BaseApiController
{
    private readonly ProfileService _profiles;

    public ProfilesController(ProfileService profiles)
    {
        _profiles = profiles;
    }

    /// <summary>Retrieves the logged-in Participant's own profile.</summary>
    /// <response code="200">Profile data (never includes the password hash).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id is not your own, or you are not a Participant.</response>
    [HttpGet("participants/{id:int}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetParticipant(int id)
    {
        var check = RequireRole(Roles.Participant) ?? RequireSelf(id);
        if (check != null) return check;

        return Respond(await _profiles.GetParticipantAsync(id));
    }

    /// <summary>Updates the logged-in Participant's own profile (name and phone number).</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id is not your own, or you are not a Participant.</response>
    [HttpPut("participants/{id:int}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateParticipant(int id, UpdateProfileRequest request)
    {
        var check = RequireRole(Roles.Participant) ?? RequireSelf(id);
        if (check != null) return check;

        return Respond(await _profiles.UpdateParticipantAsync(id, request));
    }

    /// <summary>Retrieves the logged-in Organiser's own profile.</summary>
    /// <response code="200">Profile data (never includes the password hash).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id is not your own, or you are not an Organiser.</response>
    [HttpGet("organisers/{id:int}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetOrganiser(int id)
    {
        var check = RequireRole(Roles.Organiser) ?? RequireSelf(id);
        if (check != null) return check;

        return Respond(await _profiles.GetOrganiserAsync(id));
    }

    /// <summary>Updates the logged-in Organiser's own profile (name and phone number).</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id is not your own, or you are not an Organiser.</response>
    [HttpPut("organisers/{id:int}")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateOrganiser(int id, UpdateProfileRequest request)
    {
        var check = RequireRole(Roles.Organiser) ?? RequireSelf(id);
        if (check != null) return check;

        return Respond(await _profiles.UpdateOrganiserAsync(id, request));
    }
}