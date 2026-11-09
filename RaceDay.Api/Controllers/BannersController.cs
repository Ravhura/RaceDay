using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Event banner images, stored in Azure Blob Storage.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class BannersController : BaseApiController
{
    private readonly BannerService _banners;

    public BannersController(BannerService banners)
    {
        _banners = banners;
    }

    /// <summary>Uploads a banner image for an event the Organiser manages. The image is stored in Azure Blob Storage.</summary>
    /// <remarks>Send the image as multipart/form-data in a field called "file". JPG, PNG or WebP, up to 5 MB. Replaces any existing banner.</remarks>
    /// <response code="200">Banner saved; returns the updated event including its BannerUrl.</response>
    /// <response code="400">No file, wrong file type, or the file is too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">You do not manage this event, or you are not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="503">Cloud storage is not available right now.</response>
    [HttpPost("events/{id:int}/banner")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> UploadBanner(int id, IFormFile file)
    {
        var check = RequireRole(Roles.Organiser);
        if (check != null) return check;

        return Respond(await _banners.UploadAsync(UserId, id, file));
    }
}