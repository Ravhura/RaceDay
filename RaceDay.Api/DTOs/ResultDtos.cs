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

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    /// <summary>The date of the event the result belongs to.</summary>
    public DateOnly EventDate { get; set; }

    /// <summary>How many results have been captured for this event, so "2nd of 12 finishers" can be shown.</summary>
    public int TotalFinishers { get; set; }
}