using Microsoft.AspNetCore.Identity;

namespace RaceDay.Api.Authentication;

/// <summary>
/// Hashes and verifies passwords with the framework's PasswordHasher (salted PBKDF2).
/// Only the hasher is used - no ASP.NET Identity tables are created.
/// </summary>
public class PasswordService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string hash)
    {
        var result = _hasher.VerifyHashedPassword(null!, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}