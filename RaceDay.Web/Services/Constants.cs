namespace RaceDay.Web.Services;

/// <summary>The two roles.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>The keys used to store things in this app's session.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string ApiCookie = "ApiCookie";   // the API's own session cookie value
}