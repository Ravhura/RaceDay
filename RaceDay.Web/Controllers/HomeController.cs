using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class HomeController : AppController
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    /// <summary>The Participant's home page: upcoming events with filters.</summary>
    public async Task<IActionResult> Index(string? search, string? type, DateOnly? dateFrom, DateOnly? dateTo)
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Account");
        if (Role == Roles.Organiser) return RedirectToAction("Dashboard", "Organiser");

        var result = await _api.GetEventsAsync();
        if (!result.Success) return ApiFailed(result);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var upcoming = result.Data!.Where(e => e.EventDate >= today).ToList();

        // The filtering only decides what to show. All the data comes from the API.
        IEnumerable<EventItem> filtered = upcoming;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            filtered = filtered.Where(e =>
                e.EventName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                (e.Location ?? "").Contains(text, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            filtered = filtered.Where(e => e.EventType == type);
        }
        if (dateFrom.HasValue)
        {
            filtered = filtered.Where(e => e.EventDate >= dateFrom.Value);
        }
        if (dateTo.HasValue)
        {
            filtered = filtered.Where(e => e.EventDate <= dateTo.Value);
        }

        return View(new HomeViewModel
        {
            Events = filtered.OrderBy(e => e.EventDate).ToList(),
            TotalUpcoming = upcoming.Count,
            Search = search,
            Type = type,
            DateFrom = dateFrom,
            DateTo = dateTo
        });
    }

    public IActionResult Error() => View();
}