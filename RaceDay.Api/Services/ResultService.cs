using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Capturing results (the Organiser of the event) and viewing them (owner Participant or managing Organiser).</summary>
public class ResultService
{
    private readonly RaceDayDbContext _db;

    public ResultService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ResultResponse>> CaptureAsync(int organiserId, CaptureResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null)
        {
            return ServiceResult<ResultResponse>.NotFound("Enrolment not found.");
        }
        if (enrolment.Category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<ResultResponse>.Forbidden("You can only capture results for events you manage.");
        }
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == request.EnrolmentId))
        {
            return ServiceResult<ResultResponse>.Conflict("A result has already been captured for this enrolment.");
        }
        if (!IsValidFinishTime(request.FinishTime!.Value))
        {
            return ServiceResult<ResultResponse>.BadRequest("Finish time must be greater than 00:00:00 and less than 24 hours.");
        }

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await Query().FirstAsync(r => r.ResultId == result.ResultId);
        return ServiceResult<ResultResponse>.Created(ToResponse(saved));
    }

    public async Task<ServiceResult<ResultResponse>> UpdateAsync(int organiserId, int id, UpdateResultRequest request)
    {
        var result = await Query().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null)
        {
            return ServiceResult<ResultResponse>.NotFound("Result not found.");
        }
        if (result.Enrolment.Category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<ResultResponse>.Forbidden("You can only manage results for events you manage.");
        }
        if (!IsValidFinishTime(request.FinishTime!.Value))
        {
            return ServiceResult<ResultResponse>.BadRequest("Finish time must be greater than 00:00:00 and less than 24 hours.");
        }

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        await _db.SaveChangesAsync();

        return ServiceResult<ResultResponse>.Ok(ToResponse(result));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int organiserId, int id)
    {
        var result = await Query().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null)
        {
            return ServiceResult<bool>.NotFound("Result not found.");
        }
        if (result.Enrolment.Category.Event.OrganiserId != organiserId)
        {
            return ServiceResult<bool>.Forbidden("You can only manage results for events you manage.");
        }

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.NoContent();
    }

    public async Task<ServiceResult<ResultResponse>> GetAsync(int userId, string role, int id)
    {
        var result = await Query().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null)
        {
            return ServiceResult<ResultResponse>.NotFound("Result not found.");
        }

        // A Participant may only see their own result. An Organiser may only see results for their own events.
        bool allowed;
        if (role == Roles.Participant)
        {
            allowed = result.Enrolment.ParticipantId == userId;
        }
        else
        {
            allowed = result.Enrolment.Category.Event.OrganiserId == userId;
        }

        if (!allowed)
        {
            return ServiceResult<ResultResponse>.Forbidden("You do not have access to this result.");
        }
        return ServiceResult<ResultResponse>.Ok(ToResponse(result));
    }

    public async Task<ServiceResult<List<ResultResponse>>> ListForParticipantAsync(int participantId)
    {
        var list = await Query()
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .ToListAsync();
        return ServiceResult<List<ResultResponse>>.Ok(list.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<List<ResultResponse>>> ListForEventAsync(int organiserId, int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
        {
            return ServiceResult<List<ResultResponse>>.NotFound("Event not found.");
        }
        if (ev.OrganiserId != organiserId)
        {
            return ServiceResult<List<ResultResponse>>.Forbidden("You can only view results for events you manage.");
        }

        var list = await Query()
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.Position)
            .ToListAsync();
        return ServiceResult<List<ResultResponse>>.Ok(list.Select(ToResponse).ToList());
    }

    // The SQL TIME column only holds 00:00:00 to 23:59:59, so reject anything outside that.
    private static bool IsValidFinishTime(TimeSpan time)
    {
        return time > TimeSpan.Zero && time < TimeSpan.FromHours(24);
    }

    // Loads results together with the enrolment, participant, category and event.
    private IQueryable<Result> Query()
    {
        return _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);
    }

    private static ResultResponse ToResponse(Result r)
    {
        return new ResultResponse
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            ParticipantId = r.Enrolment.ParticipantId,
            ParticipantName = r.Enrolment.Participant.FullName,
            EventId = r.Enrolment.EventId,
            EventName = r.Enrolment.Category.Event.EventName,
            CategoryName = r.Enrolment.Category.CategoryName,
            FinishTime = r.FinishTime,
            Position = r.Position
        };
    }
}