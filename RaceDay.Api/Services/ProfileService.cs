using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Middleware;

namespace RaceDay.Api.Services;

/// <summary>View/update the logged-in user's own profile. The {id} in the URL must match the session user.</summary>
public class ProfileService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUser _user;

    public ProfileService(RaceDayDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<ProfileResponse> GetParticipantAsync(int id)
    {
        _user.EnsureIsSelf(id);
        var p = await _db.Participants.AsNoTracking().FirstOrDefaultAsync(x => x.ParticipantId == id)
                ?? throw new NotFoundException("Participant not found.");
        return new ProfileResponse(p.ParticipantId, Roles.Participant, p.FullName, p.Email, p.PhoneNumber, p.CreatedAt);
    }

    public async Task<ProfileResponse> UpdateParticipantAsync(int id, UpdateProfileRequest request)
    {
        _user.EnsureIsSelf(id);
        var p = await _db.Participants.FirstOrDefaultAsync(x => x.ParticipantId == id)
                ?? throw new NotFoundException("Participant not found.");
        p.FullName = request.FullName.Trim();
        p.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        await _db.SaveChangesAsync();
        return new ProfileResponse(p.ParticipantId, Roles.Participant, p.FullName, p.Email, p.PhoneNumber, p.CreatedAt);
    }

    public async Task<ProfileResponse> GetOrganiserAsync(int id)
    {
        _user.EnsureIsSelf(id);
        var o = await _db.Organisers.AsNoTracking().FirstOrDefaultAsync(x => x.OrganiserId == id)
                ?? throw new NotFoundException("Organiser not found.");
        return new ProfileResponse(o.OrganiserId, Roles.Organiser, o.FullName, o.Email, o.PhoneNumber, o.CreatedAt);
    }

    public async Task<ProfileResponse> UpdateOrganiserAsync(int id, UpdateProfileRequest request)
    {
        _user.EnsureIsSelf(id);
        var o = await _db.Organisers.FirstOrDefaultAsync(x => x.OrganiserId == id)
                ?? throw new NotFoundException("Organiser not found.");
        o.FullName = request.FullName.Trim();
        o.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        await _db.SaveChangesAsync();
        return new ProfileResponse(o.OrganiserId, Roles.Organiser, o.FullName, o.Email, o.PhoneNumber, o.CreatedAt);
    }
}