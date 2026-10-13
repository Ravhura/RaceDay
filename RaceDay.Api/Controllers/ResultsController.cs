using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Capture results (Organiser of the event) and view them (owner Participant / managing Organiser).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ResultsController : BaseApiController
{
    private readonly ResultService _results;

    public ResultsController(ResultService results)
    {
        _results = results;
    }

    /// <summary>Captures a result (finish time and position) for a Participant's enrolment.</summary>
    /// <response code="201">Result captured; returns the new result including its ResultId.</response>
    /// <response code="400">Invalid finish time or position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser of the event can capture results.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("results")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CaptureResult(CaptureResultRequest request)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _results.CaptureAsync(UserId, request));
    }

    /// <summary>Updates a previously captured result.</summary>
    /// <response code="200">Updated result.</response>
    /// <response code="400">Invalid finish time or position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateResult(int id, UpdateResultRequest request)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _results.UpdateAsync(UserId, id, request));
    }

    /// <summary>Deletes a captured result.</summary>
    /// <response code="204">Result deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Result not found.</response>
    [HttpDelete("results/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteResult(int id)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _results.DeleteAsync(UserId, id));
    }

    /// <summary>Retrieves a single result (the owner Participant or the managing Organiser only).</summary>
    /// <response code="200">Result details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">This result is not yours / not for an event you manage.</response>
    /// <response code="404">Result not found.</response>
    [HttpGet("results/{id:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetResult(int id)
    {
        var check = RequireLogin();
        if (check != null) return check;

        return Respond(await _results.GetAsync(UserId, Role, id));
    }

    /// <summary>Retrieves the logged-in Participant's own results.</summary>
    /// <response code="200">List of the Participant's results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The id is not your own, or you are not a Participant.</response>
    [HttpGet("participants/{id:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetParticipantResults(int id)
    {
        var check = RequireRole(Roles.Participant) ?? RequireSelf(id);
        if (check != null) return check;

        return Respond(await _results.ListForParticipantAsync(id));
    }

    /// <summary>Retrieves all results for an event the Organiser manages.</summary>
    /// <response code="200">List of results, ordered by position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event, or you are not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}/results")] 
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEventResults(int id)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _results.ListForEventAsync(UserId, id));
    }
}