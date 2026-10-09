namespace RaceDay.Api.Models;

/// <summary>Allowed Event.Status values (Part 1 default: Upcoming).</summary>
public static class EventStatuses
{
    public const string Upcoming = "Upcoming";
}

/// <summary>Allowed Enrolment.Status values (Part 1 default: Confirmed; Part 3 colour-codes Pending).</summary>
public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Pending = "Pending";
}