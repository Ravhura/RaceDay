using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (any logged-in user) and manage them (the Organiser who owns the event).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class EventsController : ControllerBase
{
    private readonly EventService _events;

    public EventsController(EventService events) => _events = events;

    /// <summary>Retrieves all events so users can browse them.</summary>
    /// <response code="200">List of events, soonest first.</response>
    [HttpGet("events")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventResponse>>> GetEvents() => Ok(await _events.ListAsync());

    /// <summary>Retrieves the details of a single event.</summary>
    /// <response code="200">Event details.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(int id) => Ok(await _events.GetAsync(id));

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <response code="201">Event created; returns the new event including its EventId.</response>
    /// <response code="400">Invalid input (e.g. unknown event type, past date).</response>
    /// <response code="403">Only Organisers can create events.</response>
    [HttpPost("events")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> CreateEvent(EventRequest request)
    {
        var created = await _events.CreateAsync(request);
        return Created($"/api/events/{created.EventId}", created);
    }

    /// <summary>Updates an event managed by the logged-in Organiser.</summary>
    /// <response code="200">Updated event.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("events/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, EventRequest request) =>
        Ok(await _events.UpdateAsync(id, request));

    /// <summary>Deletes an event managed by the logged-in Organiser (and its categories).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event has enrolments and cannot be deleted.</response>
    [HttpDelete("events/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        await _events.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Retrieves all events managed by the logged-in Organiser.</summary>
    /// <response code="200">List of the Organiser's events.</response>
    /// <response code="403">The id is not your own, or you are not an Organiser.</response>
    [HttpGet("organisers/{id:int}/events")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EventResponse>>> GetOrganiserEvents(int id) =>
        Ok(await _events.ListForOrganiserAsync(id));
}