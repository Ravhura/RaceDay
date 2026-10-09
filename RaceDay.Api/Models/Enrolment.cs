namespace RaceDay.Api.Models;

/// <summary>
/// A Participant entering an Event in a chosen Category (Part 1 table: Enrolments).
/// (EventId, CategoryId) is a composite foreign key to Categories, so a category
/// from a different event cannot be selected.
/// </summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    public Participant Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}