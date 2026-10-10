using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>Details needed to create a Participant or Organiser account.</summary>
public class RegisterRequest
{
    /// <summary>Full name of the account holder.</summary>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email address. Used to log in and must be unique.</summary>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters, containing at least one letter and one digit.</summary>
    [Required, RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8-100 characters and contain at least one letter and one digit.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Credentials used to log in.</summary>
public class LoginRequest
{
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Returned when a Participant account is created.</summary>
public record ParticipantCreatedResponse(int ParticipantId, string FullName, string Email);

/// <summary>Returned when an Organiser account is created.</summary>
public record OrganiserCreatedResponse(int OrganiserId, string FullName, string Email);

/// <summary>Returned after a successful login. The session cookie is set on the response.</summary>
public record LoginResponse(int UserId, string FullName, string Email, string Role);

/// <summary>Returned by the refresh-token (session renewal) endpoint.</summary>
public record SessionRenewedResponse(int IdleTimeoutMinutes, string Message);

/// <summary>A simple confirmation message.</summary>
public record MessageResponse(string Message);