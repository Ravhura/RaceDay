using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>Details used to create or update a category (e.g. "Under 20", "10km", "21km").</summary>
public class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Distance in kilometres (optional for age-based categories).</summary>
    [Range(0.01, 999.99)]
    public decimal? DistanceKm { get; set; }

    /// <summary>Entry fee in rand. Use 0 for free entry.</summary>
    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
}