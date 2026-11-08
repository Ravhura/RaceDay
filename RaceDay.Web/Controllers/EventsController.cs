using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>A Participant looks at one event and enters it by choosing a category.</summary>
public class EventsController : ParticipantBaseController
{
    public EventsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await Api.GetEventAsync(id);
        if (!ev.Success) return ApiFailed(ev);

        var categories = await Api.GetCategoriesAsync(id);
        if (!categories.Success) return ApiFailed(categories);

        var mine = await Api.GetMyEnrolmentsAsync(UserId);
        if (!mine.Success) return ApiFailed(mine);

        return View(new EventPageViewModel
        {
            Event = ev.Data!,
            Categories = categories.Data!,
            MyEnrolments = mine.Data!.Where(e => e.EventId == id).ToList(),
            IsUpcoming = ev.Data!.EventDate >= DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category first.";
            return RedirectToAction("Details", new { id = eventId });
        }

        var result = await Api.EnrolAsync(eventId, categoryId);
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            TempData["Error"] = result.Error;   // for example: "You are already enrolled in this event and category."
            return RedirectToAction("Details", new { id = eventId });
        }

        TempData["Success"] = $"You are in! You have entered {result.Data!.EventName} ({result.Data.CategoryName}).";
        return RedirectToAction("Enrolments", "Participant");
    }
}