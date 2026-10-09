namespace RaceDay.Api.Models;

/// <summary>The race result for one Enrolment; at most one per enrolment (Part 1 table: Results).</summary>
public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
}