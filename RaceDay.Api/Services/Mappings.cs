using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Converts entities to response DTOs so entities (and password hashes) never leave the API.</summary>
public static class Mappings
{
    public static EventResponse ToResponse(this Event e) =>
        new(e.EventId, e.OrganiserId, e.EventName, e.Description, e.EventDate, e.Location, e.DistanceKm, e.EventType, e.Status);

    public static CategoryResponse ToResponse(this Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.EntryFee);

    /// <summary>Needs Participant and Category.Event to be loaded.</summary>
    public static EnrolmentResponse ToResponse(this Enrolment e) =>
        new(e.EnrolmentId, e.ParticipantId, e.Participant.FullName, e.EventId, e.Category.Event.EventName,
            e.CategoryId, e.Category.CategoryName, e.EnrolmentDate, e.Status);

    /// <summary>Needs Enrolment.Participant and Enrolment.Category.Event to be loaded.</summary>
    public static ResultResponse ToResponse(this Result r) =>
        new(r.ResultId, r.EnrolmentId, r.Enrolment.ParticipantId, r.Enrolment.Participant.FullName,
            r.Enrolment.EventId, r.Enrolment.Category.Event.EventName, r.Enrolment.Category.CategoryName,
            r.FinishTime, r.Position);
}