using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>Finish time and position for an enrolment.</summary>
public class UpdateResultRequest
{
    /// <summary>Finish time as hh:mm:ss, e.g. "00:52:30". Must be between 00:00:01 and 23:59:59.</summary>
    [Required]
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Finishing position, 1 or higher.</summary>
    [Required, Range(1, int.MaxValue)]
    public int? Position { get; set; }
}

/// <summary>Captures a new result for an enrolment.</summary>
public class CaptureResultRequest : UpdateResultRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }
}

public record ResultResponse(
    int ResultId, int EnrolmentId, int ParticipantId, string ParticipantName, int EventId, string EventName,
    string CategoryName, TimeSpan? FinishTime, int? Position);