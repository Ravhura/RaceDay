namespace RaceDay.Api.Models;

/// <summary>A category (e.g. "10km Run") that belongs to exactly one Event (Part 1 table: Categories).</summary>
public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}