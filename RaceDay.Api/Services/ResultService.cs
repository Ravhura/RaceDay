using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Capturing results (Organiser of the event) and viewing them (owner Participant / managing Organiser).</summary>
public class ResultService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUser _user;

    public ResultService(RaceDayDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<ResultResponse> CaptureAsync(CaptureResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId)
            ?? throw new NotFoundException("Enrolment not found.");

        if (enrolment.Category.Event.OrganiserId != _user.UserId)
        {
            throw new ForbiddenException("You can only capture results for events you manage.");
        }
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
        {
            throw new ConflictException("A result has already been captured for this enrolment.");
        }

        EnsureValidFinishTime(request.FinishTime!.Value);

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return (await Query().AsNoTracking().FirstAsync(r => r.ResultId == result.ResultId)).ToResponse();
    }

    public async Task<ResultResponse> UpdateAsync(int id, UpdateResultRequest request)
    {
        var result = await FindManagedAsync(id);
        EnsureValidFinishTime(request.FinishTime!.Value);
        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        await _db.SaveChangesAsync();
        return result.ToResponse();
    }

    public async Task DeleteAsync(int id)
    {
        var result = await FindManagedAsync(id);
        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
    }

    public async Task<ResultResponse> GetAsync(int id)
    {
        var result = await Query().AsNoTracking().FirstOrDefaultAsync(r => r.ResultId == id)
                     ?? throw new NotFoundException("Result not found.");

        var allowed = _user.Role == Roles.Participant
            ? result.Enrolment.ParticipantId == _user.UserId
            : result.Enrolment.Category.Event.OrganiserId == _user.UserId;
        if (!allowed)
        {
            throw new ForbiddenException("You do not have access to this result.");
        }
        return result.ToResponse();
    }

    public async Task<List<ResultResponse>> ListForParticipantAsync(int participantId)
    {
        _user.EnsureIsSelf(participantId);
        var list = await Query().AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .ToListAsync();
        return list.Select(r => r.ToResponse()).ToList();
    }

    public async Task<List<ResultResponse>> ListForEventAsync(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId)
                 ?? throw new NotFoundException("Event not found.");
        if (ev.OrganiserId != _user.UserId)
        {
            throw new ForbiddenException("You can only view results for events you manage.");
        }

        var list = await Query().AsNoTracking()
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.Position)
            .ToListAsync();
        return list.Select(r => r.ToResponse()).ToList();
    }

    private async Task<Result> FindManagedAsync(int id)
    {
        var result = await Query().FirstOrDefaultAsync(r => r.ResultId == id)
                     ?? throw new NotFoundException("Result not found.");
        if (result.Enrolment.Category.Event.OrganiserId != _user.UserId)
        {
            throw new ForbiddenException("You can only manage results for events you manage.");
        }
        return result;
    }

    // Implementation safeguard: the SQL TIME column only holds 00:00:00 - 23:59:59.
    private static void EnsureValidFinishTime(TimeSpan time)
    {
        if (time <= TimeSpan.Zero || time >= TimeSpan.FromHours(24))
        {
            throw new BadRequestException("Finish time must be greater than 00:00:00 and less than 24 hours.");
        }
    }

    private IQueryable<Result> Query() => _db.Results
        .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
        .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);
}