using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Entering events (Participant) and viewing enrolments (the owner Participant or the managing Organiser).</summary>
public class EnrolmentService
{
    private readonly RaceDayDbContext _db;

    public EnrolmentService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<EnrolmentResponse>> CreateAsync(int participantId, CreateEnrolmentRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == request.EventId);
        if (ev == null)
        {
            return ServiceResult<EnrolmentResponse>.NotFound("Event not found.");
        }

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
        if (category == null)
        {
            return ServiceResult<EnrolmentResponse>.NotFound("Category not found.");
        }

        // The selected category must belong to the selected event (the database enforces this too).
        if (category.EventId != ev.EventId)
        {
            return ServiceResult<EnrolmentResponse>.BadRequest("The selected category does not belong to this event.");
        }

        bool alreadyEnrolled = await _db.Enrolments.AnyAsync(e =>
            e.ParticipantId == participantId && e.EventId == ev.EventId && e.CategoryId == category.CategoryId);
        if (alreadyEnrolled)
        {
            return ServiceResult<EnrolmentResponse>.Conflict("You are already enrolled in this event and category.");
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId, // the logged-in Participant, taken from the session
            EventId = ev.EventId,
            CategoryId = category.CategoryId,
            Status = EnrolmentStatuses.Confirmed
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        // Load the participant name, event name and category name for the response.
        var saved = await Query().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return ServiceResult<EnrolmentResponse>.Created(ToResponse(saved));
    }

    public async Task<ServiceResult<List<EnrolmentResponse>>> ListForParticipantAsync(int participantId)
    {
        var list = await Query()
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();
        return ServiceResult<List<EnrolmentResponse>>.Ok(list.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<EnrolmentResponse>> GetAsync(int userId, string role, int id)
    {
        var enrolment = await Query().FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null)
        {
            return ServiceResult<EnrolmentResponse>.NotFound("Enrolment not found.");
        }

        // A Participant may only see their own enrolment. An Organiser may only see enrolments for their own events.
        bool allowed;
        if (role == Roles.Participant)
        {
            allowed = enrolment.ParticipantId == userId;
        }
        else
        {
            allowed = enrolment.Category.Event.OrganiserId == userId;
        }

        if (!allowed)
        {
            return ServiceResult<EnrolmentResponse>.Forbidden("You do not have access to this enrolment.");
        }
        return ServiceResult<EnrolmentResponse>.Ok(ToResponse(enrolment));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int participantId, int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null)
        {
            return ServiceResult<bool>.NotFound("Enrolment not found.");
        }
        if (enrolment.ParticipantId != participantId)
        {
            return ServiceResult<bool>.Forbidden("You can only cancel your own enrolments.");
        }
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
        {
            return ServiceResult<bool>.Conflict("A result has already been captured for this enrolment.");
        }

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.NoContent();
    }

    public async Task<ServiceResult<List<EnrolmentResponse>>> ListForEventAsync(int organiserId, int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return ServiceResult<List<EnrolmentResponse>>.NotFound("Event not found.");
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<List<EnrolmentResponse>>.Forbidden("You can only view enrolments for events you manage.");
        }

        var list = await Query().Where(e => e.EventId == eventId).OrderBy(e => e.EnrolmentDate).ToListAsync();
        return ServiceResult<List<EnrolmentResponse>>.Ok(list.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<List<EnrolmentResponse>>> ListForOrganiserAsync(int organiserId)
    {
        var list = await Query()
            .Where(e => e.Category.Event.OrganiserId == organiserId)
            .OrderBy(e => e.EventId)
            .ThenBy(e => e.EnrolmentDate)
            .ToListAsync();
        return ServiceResult<List<EnrolmentResponse>>.Ok(list.Select(ToResponse).ToList());
    }

    // Loads enrolments together with the participant, category and event, so names can be shown.
    private IQueryable<Enrolment> Query()
    {
        return _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event);
    }

    private static EnrolmentResponse ToResponse(Enrolment e)
    {
        return new EnrolmentResponse
        {
            EnrolmentId = e.EnrolmentId,
            ParticipantId = e.ParticipantId,
            ParticipantName = e.Participant.FullName,
            EventId = e.EventId,
            EventName = e.Category.Event.EventName,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.CategoryName,
            EnrolmentDate = e.EnrolmentDate,
            Status = e.Status
        };
    }
}