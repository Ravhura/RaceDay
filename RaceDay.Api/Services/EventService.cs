using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Event browsing (both roles) and event management (owning Organiser only).</summary>
public class EventService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUser _user;

    public EventService(RaceDayDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<List<EventResponse>> ListAsync()
    {
        var events = await _db.Events.AsNoTracking().OrderBy(e => e.EventDate).ToListAsync();
        return events.Select(e => e.ToResponse()).ToList();
    }

    public async Task<EventResponse> GetAsync(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id)
                 ?? throw new NotFoundException("Event not found.");
        return ev.ToResponse();
    }

    public async Task<List<EventResponse>> ListForOrganiserAsync(int organiserId)
    {
        _user.EnsureIsSelf(organiserId);
        var events = await _db.Events.AsNoTracking()
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
        return events.Select(e => e.ToResponse()).ToList();
    }

    public async Task<EventResponse> CreateAsync(EventRequest request)
    {
        EnsureNotInPast(request.EventDate!.Value);

        var ev = new Event
        {
            OrganiserId = _user.UserId, // owner comes from the session, never from the request body
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType!.Value
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return ev.ToResponse();
    }

    public async Task<EventResponse> UpdateAsync(int id, EventRequest request)
    {
        var ev = await FindOwnedAsync(id);

        var newDate = request.EventDate!.Value;
        if (newDate != ev.EventDate)
        {
            EnsureNotInPast(newDate); // only checked when the date actually changes
        }

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = newDate;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType!.Value;
        await _db.SaveChangesAsync();
        return ev.ToResponse();
    }

    public async Task DeleteAsync(int id)
    {
        var ev = await FindOwnedAsync(id);

        // The Part 1 schema has no cascade, so enrolled events are protected.
        if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
        {
            throw new ConflictException("This event has enrolments and cannot be deleted.");
        }

        // No enrolments: remove the event's categories together with the event.
        var categories = await _db.Categories.Where(c => c.EventId == id).ToListAsync();
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
    }

    private async Task<Event> FindOwnedAsync(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id)
                 ?? throw new NotFoundException("Event not found.");
        if (ev.OrganiserId != _user.UserId)
        {
            throw new ForbiddenException("You can only manage events you created.");
        }
        return ev;
    }

    // Implementation safeguard (not a POE rule): new/changed dates must not be in the past.
    private static void EnsureNotInPast(DateOnly date)
    {
        if (date < DateOnly.FromDateTime(DateTime.Today))
        {
            throw new BadRequestException("Event date cannot be in the past.");
        }
    }
}