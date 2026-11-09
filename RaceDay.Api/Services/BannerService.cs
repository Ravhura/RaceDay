using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Services;

/// <summary>Uploads an event banner image to cloud storage and saves its address on the event.</summary>
public class BannerService
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public BannerService(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    public async Task<ServiceResult<EventResponse>> UploadAsync(int organiserId, int eventId, IFormFile? file)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return ServiceResult<EventResponse>.NotFound("Event not found.");
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<EventResponse>.Forbidden("You can only upload banners for events you manage.");
        }

        if (file == null || file.Length == 0)
        {
            return ServiceResult<EventResponse>.BadRequest("Please choose an image file.");
        }
        if (file.Length > MaxBytes)
        {
            return ServiceResult<EventResponse>.BadRequest("The image must be 5 MB or smaller.");
        }
        if (!AllowedTypes.TryGetValue(file.ContentType ?? "", out var extension))
        {
            return ServiceResult<EventResponse>.BadRequest("Only JPG, PNG or WebP images are allowed.");
        }
        // The file type sent by the browser can be faked, so also check the first bytes of the file.
        if (!await HasImageSignatureAsync(file))
        {
            return ServiceResult<EventResponse>.BadRequest("That file does not look like a valid image.");
        }

        string url;
        try
        {
            await using var stream = file.OpenReadStream();
            var blobName = $"event-{eventId}-{Guid.NewGuid():N}{extension}";
            url = await _blobs.UploadAsync(stream, file.ContentType!, blobName);
        }
        catch (Exception)
        {
            // Azure unreachable or not configured. Do not show technical details to the user.
            return new ServiceResult<EventResponse>
            {
                StatusCode = 503,
                Message = "The image could not be saved to cloud storage right now. Please try again later."
            };
        }

        var oldUrl = ev.BannerUrl;
        ev.BannerUrl = url;
        await _db.SaveChangesAsync();

        // Tidy up: remove the banner that was replaced. A failure here does not matter.
        if (!string.IsNullOrEmpty(oldUrl))
        {
            try { await _blobs.DeleteAsync(oldUrl); } catch (Exception) { }
        }

        return ServiceResult<EventResponse>.Ok(EventService.ToResponse(ev));
    }

    // JPEG starts with FF D8 FF, PNG with 89 50 4E 47, and WebP is "RIFF" ... "WEBP".
    private static async Task<bool> HasImageSignatureAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        int read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        if (read < 12)
        {
            return false;
        }

        bool jpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        bool png = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        bool webp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                    && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
        return jpeg || png || webp;
    }
}