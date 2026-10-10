using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Entering events (Participant) and viewing enrolments (owner Participant / managing Organiser).</summary>
public class EnrolmentService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUser _user;

    public EnrolmentService(RaceDayDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<EnrolmentResponse> CreateAsync(CreateEnrolmentRequest request)
    {
        var participantId = _user.UserId; // from the session, never from the request body

        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == request.EventId)
                 ?? throw new NotFoundException("Event not found.");
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId)
                       ?? throw new NotFoundException("Category not found.");

        // The same rule the database enforces with the composite (EventId, CategoryId) foreign key.
        if (category.EventId != ev.EventId)
        {
            throw new BadRequestException("The selected category does not belong to this event.");
        }

        var duplicate = await _db.Enrolments.AnyAsync(e =>
            e.ParticipantId == participantId && e.EventId == ev.EventId && e.CategoryId == category.CategoryId);
        if (duplicate)
        {
            throw new ConflictException("You are already enrolled in this event and category.");
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = ev.EventId,
            CategoryId = category.CategoryId,
            Status = EnrolmentStatuses.Confirmed
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return (await Query().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId)).ToResponse();
    }

    public async Task<List<EnrolmentResponse>> ListForParticipantAsync(int participantId)
    {
        _user.EnsureIsSelf(participantId);
        var list = await Query().Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolmentDate).ToListAsync();
        return list.Select(e => e.ToResponse()).ToList();
    }

    public async Task<EnrolmentResponse> GetAsync(int id)
    {
        var enrolment = await Query().FirstOrDefaultAsync(e => e.EnrolmentId == id)
                        ?? throw new NotFoundException("Enrolment not found.");

        // Participants see only their own; Organisers see only enrolments for events they manage.
        var allowed = _user.Role == Roles.Participant
            ? enrolment.ParticipantId == _user.UserId
            : enrolment.Category.Event.OrganiserId == _user.UserId;
        if (!allowed)
        {
            throw new ForbiddenException("You do not have access to this enrolment.");
        }
        return enrolment.ToResponse();
    }

    public async Task DeleteAsync(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id)
                        ?? throw new NotFoundException("Enrolment not found.");
        if (enrolment.ParticipantId != _user.UserId)
        {
            throw new ForbiddenException("You can only cancel your own enrolments.");
        }
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
        {
            throw new ConflictException("A result has already been captured for this enrolment.");
        }
        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
    }

    public async Task<List<EnrolmentResponse>> ListForEventAsync(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId)
                 ?? throw new NotFoundException("Event not found.");
        if (ev.OrganiserId != _user.UserId)
        {
            throw new ForbiddenException("You can only view enrolments for events you manage.");
        }

        var list = await Query().Where(e => e.EventId == eventId).OrderBy(e => e.EnrolmentDate).ToListAsync();
        return list.Select(e => e.ToResponse()).ToList();
    }

    public async Task<List<EnrolmentResponse>> ListForOrganiserAsync(int organiserId)
    {
        _user.EnsureIsSelf(organiserId);
        var list = await Query().Where(e => e.Category.Event.OrganiserId == organiserId)
            .OrderBy(e => e.EventId).ThenBy(e => e.EnrolmentDate).ToListAsync();
        return list.Select(e => e.ToResponse()).ToList();
    }

    private IQueryable<Enrolment> Query() => _db.Enrolments.AsNoTracking()
        .Include(e => e.Participant)
        .Include(e => e.Category).ThenInclude(c => c.Event);
}