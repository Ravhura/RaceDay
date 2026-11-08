using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Capture, edit and delete finish times and positions for one of the Organiser's events.</summary>
public class ManageResultsController : OrganiserBaseController
{
    public ManageResultsController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success) return ApiFailed(enrolments);

        var results = await Api.GetEventResultsAsync(eventId);
        if (!results.Success) return ApiFailed(results);

        // Match each enrolment with its result (if one has been captured yet).
        var rows = enrolments.Data!
            .Select(e => new ResultRow
            {
                Enrolment = e,
                Result = results.Data!.FirstOrDefault(r => r.EnrolmentId == e.EnrolmentId)
            })
            .OrderBy(r => r.Result?.Position ?? int.MaxValue)
            .ThenBy(r => r.Enrolment.ParticipantName)
            .ToList();

        return View(new ManageResultsViewModel { Event = ev!, Rows = rows });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int enrolmentId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success) return ApiFailed(enrolments);

        var enrolment = enrolments.Data!.FirstOrDefault(e => e.EnrolmentId == enrolmentId);
        if (enrolment == null)
        {
            TempData["Error"] = "That enrolment could not be found for this event.";
            return RedirectToAction("Index", new { eventId });
        }

        var results = await Api.GetEventResultsAsync(eventId);
        if (!results.Success) return ApiFailed(results);
        var existing = results.Data!.FirstOrDefault(r => r.EnrolmentId == enrolmentId);

        return View(new ResultFormViewModel
        {
            EventId = eventId,
            EnrolmentId = enrolmentId,
            ResultId = existing?.ResultId ?? 0,
            ParticipantName = enrolment.ParticipantName,
            CategoryName = enrolment.CategoryName,
            FinishTime = existing?.FinishTime != null ? Format.Time(existing.FinishTime) : "",
            Position = existing?.Position
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ResultFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // The pattern on the form guarantees this parses. "c" writes it as hh:mm:ss.
        var time = TimeSpan.Parse(model.FinishTime, CultureInfo.InvariantCulture).ToString("c", CultureInfo.InvariantCulture);

        ApiResult<ResultItem> result = model.ResultId > 0
            ? await Api.UpdateResultAsync(model.ResultId, new { finishTime = time, position = model.Position!.Value })
            : await Api.CaptureResultAsync(new { enrolmentId = model.EnrolmentId, finishTime = time, position = model.Position!.Value });

        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        TempData["Success"] = "Result saved. The participant can see it straight away.";
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteResultAsync(id);
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = "Result deleted.";
        }
        return RedirectToAction("Index", new { eventId });
    }
}