using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Middleware;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Registration and credential checking for both roles.</summary>
public class AuthService
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordService _passwords;

    public AuthService(RaceDayDbContext db, PasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    public async Task<Participant> RegisterParticipantAsync(RegisterRequest request)
    {
        var email = Normalise(request.Email);
        if (await _db.Participants.AnyAsync(p => p.Email == email))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var participant = new Participant
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwords.Hash(request.Password)
        };
        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();
        return participant;
    }

    public async Task<Organiser> RegisterOrganiserAsync(RegisterRequest request)
    {
        var email = Normalise(request.Email);
        if (await _db.Organisers.AnyAsync(o => o.Email == email))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var organiser = new Organiser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwords.Hash(request.Password)
        };
        _db.Organisers.Add(organiser);
        await _db.SaveChangesAsync();
        return organiser;
    }

    public async Task<Participant> LoginParticipantAsync(LoginRequest request)
    {
        var email = Normalise(request.Email);
        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.Email == email);
        if (participant is null || !_passwords.Verify(request.Password, participant.PasswordHash))
        {
            // Same message for "unknown email" and "wrong password" so attackers learn nothing.
            throw new UnauthorizedException("Invalid email or password.");
        }
        return participant;
    }

    public async Task<Organiser> LoginOrganiserAsync(LoginRequest request)
    {
        var email = Normalise(request.Email);
        var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser is null || !_passwords.Verify(request.Password, organiser.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }
        return organiser;
    }

    private static string Normalise(string email) => email.Trim().ToLowerInvariant();
}