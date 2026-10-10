using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Enter events (Participants) and view enrolments (owner Participant / managing Organiser).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class EnrolmentsController : ControllerBase
{
    private readonly EnrolmentService _enrolments;

    public EnrolmentsController(EnrolmentService enrolments) => _enrolments = enrolments;

    /// <summary>Enters the logged-in Participant into an event by selecting a category.</summary>
    /// <response code="201">Enrolment created; records the Participant, Event and Category.</response>
    /// <response code="400">The category does not belong to the event.</response>
    /// <response code="404">Event or category not found.</response>
    /// <response code="409">You are already enrolled in this event and category.</response>
    [HttpPost("enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> CreateEnrolment(CreateEnrolmentRequest request)
    {
        var created = await _enrolments.CreateAsync(request);
        return Created($"/api/enrolments/{created.EnrolmentId}", created);
    }

    /// <summary>Retrieves the logged-in Participant's own enrolments.</summary>
    /// <response code="200">List of the Participant's enrolments.</response>
    /// <response code="403">The id is not your own, or you are not a Participant.</response>
    [HttpGet("participants/{id:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetParticipantEnrolments(int id) =>
        Ok(await _enrolments.ListForParticipantAsync(id));

    /// <summary>Retrieves a single enrolment (the owner Participant or the managing Organiser only).</summary>
    /// <response code="200">Enrolment details.</response>
    /// <response code="403">This enrolment is not yours / not for an event you manage.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpGet("enrolments/{id:int}")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentResponse>> GetEnrolment(int id) => Ok(await _enrolments.GetAsync(id));

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="403">This is not your enrolment, or you are not a Participant.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEnrolment(int id)
    {
        await _enrolments.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Retrieves all enrolments for an event the Organiser manages.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="403">You do not manage this event, or you are not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetEventEnrolments(int id) =>
        Ok(await _enrolments.ListForEventAsync(id));

    /// <summary>Retrieves all enrolments across every event the Organiser manages.</summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="403">The id is not your own, or you are not an Organiser.</response>
    [HttpGet("organisers/{id:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetOrganiserEnrolments(int id) =>
        Ok(await _enrolments.ListForOrganiserAsync(id));
}