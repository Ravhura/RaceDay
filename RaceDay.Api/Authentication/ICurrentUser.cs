using System.Security.Claims;
using RaceDay.Api.Middleware;

namespace RaceDay.Api.Authentication;

/// <summary>The logged-in user, taken from the server-side session (never from the URL).</summary>
public interface ICurrentUser
{
    int UserId { get; }
    string Role { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal Principal =>
        _accessor.HttpContext?.User ?? throw new InvalidOperationException("There is no active HTTP request.");

    public int UserId
    {
        get
        {
            var value = Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : throw new InvalidOperationException("Not authenticated.");
        }
    }

    public string Role =>
        Principal.FindFirst(ClaimTypes.Role)?.Value ?? throw new InvalidOperationException("Not authenticated.");
}

public static class CurrentUserExtensions
{
    /// <summary>
    /// Ownership rule: an {id} in the URL is NOT proof of identity. It must equal the session user's ID.
    /// </summary>
    public static void EnsureIsSelf(this ICurrentUser user, int requestedId)
    {
        if (user.UserId != requestedId)
        {
            throw new ForbiddenException("You can only access your own data.");
        }
    }
}