using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>A Participant's own enrolments, results and profile. They only ever see their own data.</summary>
public class ParticipantController : ParticipantBaseController
{
    public ParticipantController(ApiClient api) : base(api) { }

    // ---------- My enrolments ----------

    public async Task<IActionResult> Enrolments()
    {
        var enrolments = await Api.GetMyEnrolmentsAsync(UserId);
        if (!enrolments.Success) return ApiFailed(enrolments);

        var events = await Api.GetEventsAsync();
        if (!events.Success) return ApiFailed(events);

        var byId = events.Data!.ToDictionary(e => e.EventId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rows = enrolments.Data!
            .Select(e => new MyEnrolmentRow { Enrolment = e, Event = byId.TryGetValue(e.EventId, out var ev) ? ev : null })
            .ToList();

        // Upcoming events first (soonest first), then past events (most recent first).
        var upcoming = rows.Where(r => r.Event != null && r.Event.EventDate >= today).OrderBy(r => r.Event!.EventDate);
        var past = rows.Where(r => r.Event == null || r.Event.EventDate < today).OrderByDescending(r => r.Event?.EventDate ?? DateOnly.MinValue);

        return View(upcoming.Concat(past).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await Api.CancelEnrolmentAsync(id);
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            TempData["Error"] = result.Error;   // for example: a result has already been captured
        }
        else
        {
            TempData["Success"] = "Your enrolment has been cancelled.";
        }
        return RedirectToAction("Enrolments");
    }

    // ---------- My results ----------

    public async Task<IActionResult> Results()
    {
        var enrolments = await Api.GetMyEnrolmentsAsync(UserId);
        if (!enrolments.Success) return ApiFailed(enrolments);

        var events = await Api.GetEventsAsync();
        if (!events.Success) return ApiFailed(events);

        var results = await Api.GetMyResultsAsync(UserId);
        if (!results.Success) return ApiFailed(results);

        var byId = events.Data!.ToDictionary(e => e.EventId);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rows = new List<MyResultRow>();

        foreach (var enrolment in enrolments.Data!)
        {
            byId.TryGetValue(enrolment.EventId, out var ev);
            var result = results.Data!.FirstOrDefault(r => r.EnrolmentId == enrolment.EnrolmentId);
            bool completed = ev != null && ev.EventDate < today;

            // Show events that have a result, and completed events still waiting for one.
            if (result == null && !completed) continue;

            rows.Add(new MyResultRow { Enrolment = enrolment, Event = ev, Result = result });
        }

        return View(rows.OrderByDescending(r => r.Event?.EventDate ?? DateOnly.MinValue).ToList());
    }

    // ---------- Profile ----------

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var profile = await Api.GetProfileAsync(Roles.Participant, UserId);
        if (!profile.Success) return ApiFailed(profile);

        return View(new ProfileViewModel
        {
            FullName = profile.Data!.FullName,
            PhoneNumber = profile.Data.PhoneNumber,
            Email = profile.Data.Email,
            MemberSince = profile.Data.CreatedAt.ToString("dd MMM yyyy")
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.UpdateProfileAsync(Roles.Participant, UserId, new
        {
            fullName = model.FullName.Trim(),
            phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
        });

        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        // Keep the name in the menu up to date.
        HttpContext.Session.SetString(SessionKeys.FullName, result.Data!.FullName);

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction("Profile");
    }
}