using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Registration and login for both roles.</summary>
public class AuthService
{
    private readonly RaceDayDbContext _db;

    public AuthService(RaceDayDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ParticipantCreatedResponse>> RegisterParticipantAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Participants.AnyAsync(p => p.Email == email))
        {
            return ServiceResult<ParticipantCreatedResponse>.Conflict("An account with this email already exists.");
        }

        var participant = new Participant
        {
            FullName = request.FullName.Trim(),
            Email = email,
            // The Identity hasher adds a random salt and hashes the password. The plain password is never stored.
            PasswordHash = PasswordHelper.Hash(request.Password)
        };
        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();

        return ServiceResult<ParticipantCreatedResponse>.Created(new ParticipantCreatedResponse
        {
            ParticipantId = participant.ParticipantId,
            FullName = participant.FullName,
            Email = participant.Email
        });
    }

    public async Task<ServiceResult<OrganiserCreatedResponse>> RegisterOrganiserAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Organisers.AnyAsync(o => o.Email == email))
        {
            return ServiceResult<OrganiserCreatedResponse>.Conflict("An account with this email already exists.");
        }

        var organiser = new Organiser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHelper.Hash(request.Password)
        };
        _db.Organisers.Add(organiser);
        await _db.SaveChangesAsync();

        return ServiceResult<OrganiserCreatedResponse>.Created(new OrganiserCreatedResponse
        {
            OrganiserId = organiser.OrganiserId,
            FullName = organiser.FullName,
            Email = organiser.Email
        });
    }

    public async Task<ServiceResult<LoginResponse>> LoginParticipantAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.Email == email);

        // Same message for "unknown email" and "wrong password" so an attacker learns nothing.
        if (participant == null || !PasswordHelper.Verify(request.Password, participant.PasswordHash))
        {
            return ServiceResult<LoginResponse>.Unauthorized("Invalid email or password.");
        }

        return ServiceResult<LoginResponse>.Ok(new LoginResponse
        {
            UserId = participant.ParticipantId,
            FullName = participant.FullName,
            Email = participant.Email,
            Role = Roles.Participant
        });
    }

    public async Task<ServiceResult<LoginResponse>> LoginOrganiserAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.Email == email);

        if (organiser == null || !PasswordHelper.Verify(request.Password, organiser.PasswordHash))
        {
            return ServiceResult<LoginResponse>.Unauthorized("Invalid email or password.");
        }

        return ServiceResult<LoginResponse>.Ok(new LoginResponse
        {
            UserId = organiser.OrganiserId,
            FullName = organiser.FullName,
            Email = organiser.Email,
            Role = Roles.Organiser
        });
    }
}