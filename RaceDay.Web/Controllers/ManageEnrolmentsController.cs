using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Shows who has enrolled for one of the Organiser's events.</summary>
public class ManageEnrolmentsController : OrganiserBaseController
{
    public ManageEnrolmentsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success) return ApiFailed(enrolments);

        return View(new ManageEnrolmentsViewModel { Event = ev!, Enrolments = enrolments.Data! });
    }
}