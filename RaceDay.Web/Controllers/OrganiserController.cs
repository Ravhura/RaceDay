using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class OrganiserController : OrganiserBaseController
{
    public OrganiserController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Dashboard()
    {
        var rows = await LoadEventRowsAsync();
        if (!rows.Success) return ApiFailed(rows);

        return View(new DashboardViewModel
        {
            Events = rows.Data!,
            TotalEnrolments = rows.Data!.Sum(r => r.Enrolments)
        });
    }
}