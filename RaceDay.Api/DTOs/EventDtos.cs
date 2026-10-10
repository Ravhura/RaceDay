using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.DTOs;

/// <summary>Details used to create or update an event.</summary>
public class EventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Event date (yyyy-MM-dd).</summary>
    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Overall event distance in kilometres.</summary>
    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required, EnumDataType(typeof(EventType))]
    public EventType? EventType { get; set; }
}

public record EventResponse(
    int EventId, int OrganiserId, string EventName, string Description, DateOnly EventDate,
    string? Location, decimal DistanceKm, EventType EventType, string Status);