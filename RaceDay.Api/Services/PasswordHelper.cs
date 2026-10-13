using Microsoft.AspNetCore.Identity;

namespace RaceDay.Api.Services;

/// <summary>One place for hashing and checking passwords (ASP.NET Core Identity hasher, PBKDF2 with a random salt).</summary>
public static class PasswordHelper
{
    private static readonly PasswordHasher<object> Hasher = new();

    public static string Hash(string password) => Hasher.HashPassword(new object(), password);

    // Returns false (never throws) if the stored hash is missing or not a valid Identity hash.
    public static bool Verify(string password, string? storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash)) return false;
        try
        {
            var result = Hasher.VerifyHashedPassword(new object(), storedHash, password);
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}