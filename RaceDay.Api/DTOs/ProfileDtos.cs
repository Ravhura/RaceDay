using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>Fields a user may change on their own profile.</summary>
public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Optional. Digits and spaces, optionally starting with +.</summary>
    [RegularExpression(@"^\+?[0-9 ]{7,20}$", ErrorMessage = "Phone number must be 7-20 digits (spaces and a leading + are allowed).")]
    public string? PhoneNumber { get; set; }
}

/// <summary>A user's own profile. The password hash is never included.</summary>
public record ProfileResponse(int Id, string Role, string FullName, string Email, string? PhoneNumber, DateTime CreatedAt);