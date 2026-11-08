using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Create, view, edit and delete the Organiser's events, including the banner image.</summary>
public class ManageEventsController : OrganiserBaseController
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public ManageEventsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var rows = await LoadEventRowsAsync();
        if (!rows.Success) return ApiFailed(rows);
        return View(rows.Data);
    }

    public async Task<IActionResult> Details(int id)
    {
        var (ev, failure) = await LoadOwnEventAsync(id);
        if (failure != null) return failure;

        var categories = await Api.GetCategoriesAsync(id);
        if (!categories.Success) return ApiFailed(categories);

        var enrolments = await Api.GetEventEnrolmentsAsync(id);
        if (!enrolments.Success) return ApiFailed(enrolments);

        return View(new EventDetailsViewModel
        {
            Event = ev!,
            Categories = categories.Data!,
            EnrolmentCount = enrolments.Data!.Count
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EventFormViewModel { EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        var bannerProblem = CheckBanner(model.BannerFile);
        if (bannerProblem != null) ModelState.AddModelError(nameof(model.BannerFile), bannerProblem);
        if (!ModelState.IsValid) return View(model);

        var result = await Api.CreateEventAsync(ToApiBody(model));
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        TempData["Success"] = "Event created.";
        await UploadBannerIfChosenAsync(result.Data!.EventId, model.BannerFile);
        return RedirectToAction("Details", new { id = result.Data!.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, failure) = await LoadOwnEventAsync(id);
        if (failure != null) return failure;

        return View(new EventFormViewModel
        {
            EventId = ev!.EventId,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location ?? "",
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            BannerUrl = ev.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventFormViewModel model)
    {
        var bannerProblem = CheckBanner(model.BannerFile);
        if (bannerProblem != null) ModelState.AddModelError(nameof(model.BannerFile), bannerProblem);
        if (!ModelState.IsValid) return View(model);

        var result = await Api.UpdateEventAsync(model.EventId, ToApiBody(model));
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        TempData["Success"] = "Event updated.";
        await UploadBannerIfChosenAsync(model.EventId, model.BannerFile);
        return RedirectToAction("Details", new { id = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            TempData["Error"] = result.Error;   // for example: "This event has enrolments and cannot be deleted."
            return RedirectToAction("Index");
        }

        TempData["Success"] = "Event deleted.";
        return RedirectToAction("Index");
    }

    // ---------- helpers ----------

    private static object ToApiBody(EventFormViewModel m) => new
    {
        eventName = m.EventName.Trim(),
        description = m.Description.Trim(),
        eventDate = m.EventDate!.Value.ToString("yyyy-MM-dd"),
        location = m.Location.Trim(),
        distanceKm = m.DistanceKm!.Value,
        eventType = m.EventType
    };

    /// <summary>Quick friendly checks before sending the file. The API checks again.</summary>
    private static string? CheckBanner(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxImageBytes) return "The image must be 5 MB or smaller.";
        if (!AllowedImageTypes.Contains(file.ContentType)) return "Please choose a JPG, PNG or WebP image.";
        return null;
    }

    /// <summary>Sends the banner to the API (which stores it in Azure Blob Storage). A failure does not undo the event.</summary>
    private async Task UploadBannerIfChosenAsync(int eventId, IFormFile? file)
    {
        if (file == null || file.Length == 0) return;

        var upload = await Api.UploadBannerAsync(eventId, file);
        if (!upload.Success)
        {
            TempData["Error"] = "The event was saved, but the banner could not be uploaded: " + upload.Error;
        }
    }
}