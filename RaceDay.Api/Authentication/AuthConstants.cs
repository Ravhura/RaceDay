namespace RaceDay.Api.Authentication;

/// <summary>The two functional roles in RaceDay.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Keys used to store the logged-in user inside the server-side session.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}