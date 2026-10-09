namespace RaceDay.Api.Models;

/// <summary>Event Organiser account (Part 1 table: Organisers).</summary>
public class Organiser
{
    public int OrganiserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}