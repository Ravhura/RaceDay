using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Capture results (Organiser of the event) and view them (owner Participant / managing Organiser).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ResultsController : ControllerBase
{
    private readonly ResultService _results;

    public ResultsController(ResultService results) => _results = results;

    /// <summary>Captures a result (finish time and position) for a Participant's enrolment.</summary>
    /// <response code="201">Result captured; returns the new result including its ResultId.</response>
    /// <response code="400">Invalid finish time or position.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> CaptureResult(CaptureResultRequest request)
    {
        var created = await _results.CaptureAsync(request);
        return Created($"/api/results/{created.ResultId}", created);
    }

    /// <summary>Updates a previously captured result.</summary>
    /// <response code="200">Updated result.</response>
    /// <response code="400">Invalid finish time or position.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> UpdateResult(int id, UpdateResultRequest request) =>
        Ok(await _results.UpdateAsync(id, request));

    /// <summary>Deletes a captured result.</summary>
    /// <response code="204">Result deleted.</response>
    /// <response code="404">Result not found.</response>
    [HttpDelete("results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResult(int id)
    {
        await _results.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Retrieves a single result (the owner Participant or the managing Organiser only).</summary>
    /// <response code="200">Result details.</response>
    /// <response code="403">This result is not yours / not for an event you manage.</response>
    /// <response code="404">Result not found.</response>
    [HttpGet("results/{id:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> GetResult(int id) => Ok(await _results.GetAsync(id));

    /// <summary>Retrieves the logged-in Participant's own results.</summary>
    /// <response code="200">List of the Participant's results.</response>
    /// <response code="403">The id is not your own, or you are not a Participant.</response>
    [HttpGet("participants/{id:int}/results")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ResultResponse>>> GetParticipantResults(int id) =>
        Ok(await _results.ListForParticipantAsync(id));

    /// <summary>Retrieves all results for an event the Organiser manages.</summary>
    /// <response code="200">List of results, ordered by position.</response>
    /// <response code="403">You do not manage this event, or you are not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultResponse>>> GetEventResults(int id) =>
        Ok(await _results.ListForEventAsync(id));
}