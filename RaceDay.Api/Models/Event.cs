namespace RaceDay.Api.Models;

/// <summary>
/// A race event managed by one Organiser (Part 1 table: Events).
/// Description, DistanceKm and EventType are the additive Part 2 fields.
/// </summary>
public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string? Location { get; set; }
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public string Status { get; set; } = EventStatuses.Upcoming;

    public Organiser Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    // Deliberately no Enrolments collection: in the Part 1 schema, Enrolments
    // reaches Events only through the composite (EventId, CategoryId) key on Categories.
}