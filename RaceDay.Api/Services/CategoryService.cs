using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Category viewing (anyone logged in) and management (the Organiser who owns the event).</summary>
public class CategoryService
{
    private readonly RaceDayDbContext _db;

    public CategoryService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<List<CategoryResponse>>> ListForEventAsync(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return ServiceResult<List<CategoryResponse>>.NotFound("Event not found.");
        }

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
        return ServiceResult<List<CategoryResponse>>.Ok(categories.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<CategoryResponse>> GetAsync(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return ServiceResult<CategoryResponse>.NotFound("Category not found.");
        }
        return ServiceResult<CategoryResponse>.Ok(ToResponse(category));
    }

    public async Task<ServiceResult<CategoryResponse>> CreateAsync(int organiserId, int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return ServiceResult<CategoryResponse>.NotFound("Event not found.");
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<CategoryResponse>.Forbidden("You can only add categories to events you manage.");
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return ServiceResult<CategoryResponse>.Created(ToResponse(category));
    }

    public async Task<ServiceResult<CategoryResponse>> UpdateAsync(int organiserId, int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return ServiceResult<CategoryResponse>.NotFound("Category not found.");
        }
        if (category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<CategoryResponse>.Forbidden("You can only manage categories for events you manage.");
        }

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        await _db.SaveChangesAsync();

        return ServiceResult<CategoryResponse>.Ok(ToResponse(category));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int organiserId, int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
        {
            return ServiceResult<bool>.NotFound("Category not found.");
        }
        if (category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<bool>.Forbidden("You can only manage categories for events you manage.");
        }
        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
        {
            return ServiceResult<bool>.Conflict("This category has enrolments and cannot be deleted.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.NoContent();
    }

    private static CategoryResponse ToResponse(Category c)
    {
        return new CategoryResponse
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            CategoryName = c.CategoryName,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee
        };
    }
}