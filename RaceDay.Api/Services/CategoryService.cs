using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Category viewing (both roles) and management (Organiser who owns the event).</summary>
public class CategoryService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUser _user;

    public CategoryService(RaceDayDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<List<CategoryResponse>> ListForEventAsync(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            throw new NotFoundException("Event not found.");
        }

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
        return categories.Select(c => c.ToResponse()).ToList();
    }

    public async Task<CategoryResponse> GetAsync(int id)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id)
                       ?? throw new NotFoundException("Category not found.");
        return category.ToResponse();
    }

    public async Task<CategoryResponse> CreateAsync(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId)
                 ?? throw new NotFoundException("Event not found.");
        if (ev.OrganiserId != _user.UserId)
        {
            throw new ForbiddenException("You can only add categories to events you manage.");
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
        return category.ToResponse();
    }

    public async Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request)
    {
        var category = await FindOwnedAsync(id);
        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        await _db.SaveChangesAsync();
        return category.ToResponse();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await FindOwnedAsync(id);
        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
        {
            throw new ConflictException("This category has enrolments and cannot be deleted.");
        }
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }

    private async Task<Category> FindOwnedAsync(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id)
                       ?? throw new NotFoundException("Category not found.");
        if (category.Event.OrganiserId != _user.UserId)
        {
            throw new ForbiddenException("You can only manage categories for events you manage.");
        }
        return category;
    }
}