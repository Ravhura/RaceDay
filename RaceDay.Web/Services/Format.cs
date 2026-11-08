using System.Globalization;

namespace RaceDay.Web.Services;

/// <summary>Turns values into the text shown on the pages.</summary>
public static class Format
{
    public static string Money(decimal amount) => "R" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    public static string Date(DateOnly date) => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    public static string Time(TimeSpan? time) =>
        time.HasValue ? time.Value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) : "-";

    /// <summary>1 becomes 1st, 2 becomes 2nd, 11 becomes 11th, 47 becomes 47th.</summary>
    public static string Ordinal(int? number)
    {
        if (number == null) return "-";
        int n = number.Value;
        if (n % 100 >= 11 && n % 100 <= 13) return n + "th";
        return (n % 10) switch
        {
            1 => n + "st",
            2 => n + "nd",
            3 => n + "rd",
            _ => n + "th"
        };
    }

    public static string DaysUntil(DateOnly date)
    {
        int days = date.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
        if (days == 0) return "Today";
        if (days == 1) return "Tomorrow";
        return $"In {days} days";
    }

    public static string TypeIcon(string eventType) => eventType switch
    {
        "Run" => "bi-lightning-charge",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag"
    };
}