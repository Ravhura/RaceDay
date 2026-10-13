using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Event browsing (anyone logged in) and event management (the Organiser who owns the event).</summary>
public class EventService
{
    private readonly RaceDayDbContext _db;

    public EventService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<List<EventResponse>>> ListAsync()
    {
        var events = await _db.Events.OrderBy(e => e.EventDate).ToListAsync();
        return ServiceResult<List<EventResponse>>.Ok(events.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<EventResponse>> GetAsync(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return ServiceResult<EventResponse>.NotFound("Event not found.");
        }
        return ServiceResult<EventResponse>.Ok(ToResponse(ev));
    }

    public async Task<ServiceResult<List<EventResponse>>> ListForOrganiserAsync(int organiserId)
    {
        var events = await _db.Events
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
        return ServiceResult<List<EventResponse>>.Ok(events.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<EventResponse>> CreateAsync(int organiserId, EventRequest request)
    {
        // Extra safeguard (not a POE rule): a new event cannot be dated in the past.
        if (request.EventDate!.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            return ServiceResult<EventResponse>.BadRequest("Event date cannot be in the past.");
        }

        var ev = new Event
        {
            OrganiserId = organiserId, // the owner is the logged-in Organiser, taken from the session
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType!.Value
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return ServiceResult<EventResponse>.Created(ToResponse(ev));
    }

    public async Task<ServiceResult<EventResponse>> UpdateAsync(int organiserId, int id, EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return ServiceResult<EventResponse>.NotFound("Event not found.");
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<EventResponse>.Forbidden("You can only manage events you created.");
        }

        // Only check the date if it is being changed.
        if (request.EventDate!.Value != ev.EventDate && request.EventDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            return ServiceResult<EventResponse>.BadRequest("Event date cannot be in the past.");
        }

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = request.EventDate.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType!.Value;
        await _db.SaveChangesAsync();

        return ServiceResult<EventResponse>.Ok(ToResponse(ev));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int organiserId, int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
        {
            return ServiceResult<bool>.NotFound("Event not found.");
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<bool>.Forbidden("You can only manage events you created.");
        }

        // The database has no cascade deletes (Part 1), so an event with enrolments is protected.
        if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
        {
            return ServiceResult<bool>.Conflict("This event has enrolments and cannot be deleted.");
        }

        // No enrolments: delete the event's categories first, then the event.
        var categories = await _db.Categories.Where(c => c.EventId == id).ToListAsync();
        _db.Categories.RemoveRange(categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.NoContent();
    }

    private static EventResponse ToResponse(Event e)
    {
        return new EventResponse
        {
            EventId = e.EventId,
            OrganiserId = e.OrganiserId,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            Status = e.Status
        };
    }
}