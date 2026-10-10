using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>The event and category a Participant wants to enter. The Participant is taken from the session.</summary>
public class CreateEnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public record EnrolmentResponse(
    int EnrolmentId, int ParticipantId, string ParticipantName, int EventId, string EventName,
    int CategoryId, string CategoryName, DateTime EnrolmentDate, string Status);