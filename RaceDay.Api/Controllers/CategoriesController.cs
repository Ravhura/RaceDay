using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>View categories (any logged-in user) and manage them (Organiser who owns the event).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class CategoriesController : BaseApiController
{
    private readonly CategoryService _categories;

    public CategoriesController(CategoryService categories)
    {
        _categories = categories;
    }

    /// <summary>Retrieves all categories available for a specific event.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{id:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEventCategories(int id)
    {
        var check = RequireLogin();
        if (check != null) return check;

        return Respond(await _categories.ListForEventAsync(id));
    }

    /// <summary>Retrieves the details of a single category.</summary>
    /// <response code="200">Category details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCategory(int id)
    {
        var check = RequireLogin();
        if (check != null) return check;

        return Respond(await _categories.GetAsync(id));
    }

    /// <summary>Creates a new category for an event the Organiser manages.</summary>
    /// <response code="201">Category created; returns the new category including its CategoryId.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("events/{id:int}/categories")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CreateCategory(int id, CategoryRequest request)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _categories.CreateAsync(UserId, id, request));
    }

    /// <summary>Updates a category belonging to an event the Organiser manages.</summary>
    /// <response code="200">Updated category.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _categories.UpdateAsync(UserId, id, request));
    }

    /// <summary>Deletes a category belonging to an event the Organiser manages.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category has enrolments and cannot be deleted.</response>
    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _categories.DeleteAsync(UserId, id));
    }
}