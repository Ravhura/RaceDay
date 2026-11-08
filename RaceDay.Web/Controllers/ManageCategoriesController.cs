using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Add, edit and delete the categories of one of the Organiser's events.</summary>
public class ManageCategoriesController : OrganiserBaseController
{
    public ManageCategoriesController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        var categories = await Api.GetCategoriesAsync(eventId);
        if (!categories.Success) return ApiFailed(categories);

        return View(new ManageCategoriesViewModel { Event = ev!, Categories = categories.Data! });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure != null) return failure;

        return View(new CategoryFormViewModel { EventId = eventId, EventName = ev!.EventName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.CreateCategoryAsync(model.EventId, ToApiBody(model));
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        TempData["Success"] = "Category added.";
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await Api.GetCategoryAsync(id);
        if (!category.Success) return ApiFailed(category);

        var (ev, failure) = await LoadOwnEventAsync(category.Data!.EventId);
        if (failure != null) return failure;

        return View(new CategoryFormViewModel
        {
            CategoryId = category.Data.CategoryId,
            EventId = category.Data.EventId,
            EventName = ev!.EventName,
            CategoryName = category.Data.CategoryName,
            DistanceKm = category.Data.DistanceKm,
            EntryFee = category.Data.EntryFee
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.UpdateCategoryAsync(model.CategoryId, ToApiBody(model));
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (!result.Success)
        {
            if (result.StatusCode == 401) return ApiFailed(result);
            TempData["Error"] = result.Error;   // for example: "This category has enrolments and cannot be deleted."
        }
        else
        {
            TempData["Success"] = "Category deleted.";
        }
        return RedirectToAction("Index", new { eventId });
    }

    private static object ToApiBody(CategoryFormViewModel m) => new
    {
        categoryName = m.CategoryName.Trim(),
        distanceKm = m.DistanceKm,
        entryFee = m.EntryFee!.Value
    };
}