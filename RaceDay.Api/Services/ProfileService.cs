using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Services;

/// <summary>View and update a user's profile. The controller has already checked the id is the logged-in user's own.</summary>
public class ProfileService
{
    private readonly RaceDayDbContext _db;

    public ProfileService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ProfileResponse>> GetParticipantAsync(int id)
    {
        var p = await _db.Participants.FirstOrDefaultAsync(x => x.ParticipantId == id);
        if (p == null)
        {
            return ServiceResult<ProfileResponse>.NotFound("Participant not found.");
        }

        return ServiceResult<ProfileResponse>.Ok(new ProfileResponse
        {
            Id = p.ParticipantId,
            Role = Roles.Participant,
            FullName = p.FullName,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            CreatedAt = p.CreatedAt
        });
    }

    public async Task<ServiceResult<ProfileResponse>> UpdateParticipantAsync(int id, UpdateProfileRequest request)
    {
        var p = await _db.Participants.FirstOrDefaultAsync(x => x.ParticipantId == id);
        if (p == null)
        {
            return ServiceResult<ProfileResponse>.NotFound("Participant not found.");
        }

        p.FullName = request.FullName.Trim();
        p.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        await _db.SaveChangesAsync();

        return ServiceResult<ProfileResponse>.Ok(new ProfileResponse
        {
            Id = p.ParticipantId,
            Role = Roles.Participant,
            FullName = p.FullName,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            CreatedAt = p.CreatedAt
        });
    }

    public async Task<ServiceResult<ProfileResponse>> GetOrganiserAsync(int id)
    {
        var o = await _db.Organisers.FirstOrDefaultAsync(x => x.OrganiserId == id);
        if (o == null)
        {
            return ServiceResult<ProfileResponse>.NotFound("Organiser not found.");
        }

        return ServiceResult<ProfileResponse>.Ok(new ProfileResponse
        {
            Id = o.OrganiserId,
            Role = Roles.Organiser,
            FullName = o.FullName,
            Email = o.Email,
            PhoneNumber = o.PhoneNumber,
            CreatedAt = o.CreatedAt
        });
    }

    public async Task<ServiceResult<ProfileResponse>> UpdateOrganiserAsync(int id, UpdateProfileRequest request)
    {
        var o = await _db.Organisers.FirstOrDefaultAsync(x => x.OrganiserId == id);
        if (o == null)
        {
            return ServiceResult<ProfileResponse>.NotFound("Organiser not found.");
        }

        o.FullName = request.FullName.Trim();
        o.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        await _db.SaveChangesAsync();

        return ServiceResult<ProfileResponse>.Ok(new ProfileResponse
        {
            Id = o.OrganiserId,
            Role = Roles.Organiser,
            FullName = o.FullName,
            Email = o.Email,
            PhoneNumber = o.PhoneNumber,
            CreatedAt = o.CreatedAt
        });
    }
}