using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>View categories (any logged-in user) and manage them (Organiser who owns the event).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categories;

    public CategoriesController(CategoryService categories) => _categories = categories;

    /// <summary>Retrieves all categories available for a specific event.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryResponse>>> GetEventCategories(int id) =>
        Ok(await _categories.ListForEventAsync(id));

    /// <summary>Retrieves the details of a single category.</summary>
    /// <response code="200">Category details.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetCategory(int id) => Ok(await _categories.GetAsync(id));

    /// <summary>Creates a new category for an event the Organiser manages.</summary>
    /// <response code="201">Category created; returns the new category including its CategoryId.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("events/{id:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(int id, CategoryRequest request)
    {
        var created = await _categories.CreateAsync(id, request);
        return Created($"/api/categories/{created.CategoryId}", created);
    }

    /// <summary>Updates a category belonging to an event the Organiser manages.</summary>
    /// <response code="200">Updated category.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, CategoryRequest request) =>
        Ok(await _categories.UpdateAsync(id, request));

    /// <summary>Deletes a category belonging to an event the Organiser manages.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category has enrolments and cannot be deleted.</response>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _categories.DeleteAsync(id);
        return NoContent();
    }
}