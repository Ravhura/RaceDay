using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for the Organiser pages. Only a logged-in Organiser may use anything that inherits from it.</summary>
[RequireRole(Roles.Organiser)]
public abstract class OrganiserBaseController : AppController
{
    protected readonly ApiClient Api;

    protected OrganiserBaseController(ApiClient api)
    {
        Api = api;
    }

    /// <summary>The Organiser's events, each with its number of enrolments (counted from the API's data).</summary>
    protected async Task<ApiResult<List<DashboardEventRow>>> LoadEventRowsAsync()
    {
        var events = await Api.GetOrganiserEventsAsync(UserId);
        if (!events.Success)
        {
            return ApiResult<List<DashboardEventRow>>.Fail(events.StatusCode, events.Error);
        }

        var enrolments = await Api.GetOrganiserEnrolmentsAsync(UserId);
        if (!enrolments.Success)
        {
            return ApiResult<List<DashboardEventRow>>.Fail(enrolments.StatusCode, enrolments.Error);
        }

        var counts = enrolments.Data!.GroupBy(e => e.EventId).ToDictionary(g => g.Key, g => g.Count());

        var rows = events.Data!
            .OrderBy(e => e.EventDate)
            .Select(e => new DashboardEventRow
            {
                Event = e,
                Enrolments = counts.TryGetValue(e.EventId, out var count) ? count : 0
            })
            .ToList();

        return ApiResult<List<DashboardEventRow>>.Ok(rows);
    }

    /// <summary>Loads an event and makes sure it belongs to the logged-in Organiser (the API checks this too).</summary>
    protected async Task<(EventItem? Event, IActionResult? Failure)> LoadOwnEventAsync(int eventId)
    {
        var result = await Api.GetEventAsync(eventId);
        if (!result.Success)
        {
            return (null, ApiFailed(result));
        }

        if (result.Data!.OrganiserId != UserId)
        {
            TempData["Error"] = "You can only manage events you created.";
            return (null, RedirectToAction("Index", "ManageEvents"));
        }

        return (result.Data, null);
    }
}