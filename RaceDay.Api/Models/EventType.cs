using System.Text.Json.Serialization;

namespace RaceDay.Api.Models;

/// <summary>The three event types required by the Part 2 brief. Stored as text in the database.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))] // JSON and Swagger show "Run", "Walk", "Cycle" instead of 0, 1, 2
public enum EventType
{
    Run,
    Walk,
    Cycle
}