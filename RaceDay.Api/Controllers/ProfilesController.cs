using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>View and update your own profile. The {id} must be your own ID.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ProfilesController : ControllerBase
{
    private readonly ProfileService _profiles;

    public ProfilesController(ProfileService profiles) => _profiles = profiles;

    /// <summary>Retrieves the logged-in Participant's own profile.</summary>
    /// <response code="200">Profile data (never includes the password hash).</response>
    /// <response code="403">The id is not your own, or you are not a Participant.</response>
    [HttpGet("participants/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponse>> GetParticipant(int id) =>
        Ok(await _profiles.GetParticipantAsync(id));

    /// <summary>Updates the logged-in Participant's own profile (name and phone number).</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">The id is not your own, or you are not a Participant.</response>
    [HttpPut("participants/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileResponse>> UpdateParticipant(int id, UpdateProfileRequest request) =>
        Ok(await _profiles.UpdateParticipantAsync(id, request));

    /// <summary>Retrieves the logged-in Organiser's own profile.</summary>
    /// <response code="200">Profile data (never includes the password hash).</response>
    /// <response code="403">The id is not your own, or you are not an Organiser.</response>
    [HttpGet("organisers/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponse>> GetOrganiser(int id) =>
        Ok(await _profiles.GetOrganiserAsync(id));

    /// <summary>Updates the logged-in Organiser's own profile (name and phone number).</summary>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">The id is not your own, or you are not an Organiser.</response>
    [HttpPut("organisers/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileResponse>> UpdateOrganiser(int id, UpdateProfileRequest request) =>
        Ok(await _profiles.UpdateOrganiserAsync(id, request));
}