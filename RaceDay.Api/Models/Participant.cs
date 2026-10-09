namespace RaceDay.Api.Models;

/// <summary>Participant account (Part 1 table: Participants).</summary>
public class Participant
{
    public int ParticipantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}