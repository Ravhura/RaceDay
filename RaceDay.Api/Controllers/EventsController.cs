using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (any logged-in user) and manage them (the Organiser who owns the event).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class EventsController : BaseApiController
{
    private readonly EventService _events;

    public EventsController(EventService events)
    {
        _events = events;
    }

    /// <summary>Retrieves all events so users can browse them.</summary>
    /// <response code="200">List of events, soonest first.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("events")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetEvents()
    {
        var check = RequireLogin();
        if (check != null) return check;

        return Respond(await _events.ListAsync());
    }

    /// <summary>Retrieves the details of a single event.</summary>
    /// <response code="200">Event details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEvent(int id)
    {
        var check = RequireLogin();
        if (check != null) return check;

        return Respond(await _events.GetAsync(id));
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <response code="201">Event created; returns the new event including its EventId.</response>
    /// <response code="400">Invalid input (e.g. unknown event type, past date).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create events.</response>
    [HttpPost("events")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CreateEvent(EventRequest request)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _events.CreateAsync(UserId, request));
    }

    /// <summary>Updates an event managed by the logged-in Organiser.</summary>
    /// <response code="200">Updated event.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateEvent(int id, EventRequest request)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _events.UpdateAsync(UserId, id, request));
    }

    /// <summary>Deletes an event managed by the logged-in Organiser (and its categories).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event has enrolments and cannot be deleted.</response>
    [HttpDelete("events/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _events.DeleteAsync(UserId, id));
    }

    /// <summary>Retrieves all events managed by the logged-in Organiser.</summary>
    /// <response code="200">List of the Organiser's events.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id is not your own, or you are not an Organiser.</response>
    [HttpGet("organisers/{id:int}/events")]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetOrganiserEvents(int id)
    {
        var check = RequireRole(Roles.Organiser) ?? RequireSelf(id);
        if (check != null) return check;

        return Respond(await _events.ListForOrganiserAsync(id));
    }
}