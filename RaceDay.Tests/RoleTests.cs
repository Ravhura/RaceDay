using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

/// <summary>Not logged in -> 401. Wrong role -> 403. This covers every protected endpoint.</summary>
public class RoleTests
{
    [Fact]
    public async Task NotLoggedIn_EveryProtectedEndpoint_Returns401()
    {
        using var db = TestHelper.NewDb();
        var auth = TestHelper.Auth(db);
        var profiles = TestHelper.Profiles(db);
        var events = TestHelper.Events(db);
        var categories = TestHelper.Categories(db);
        var enrolments = TestHelper.Enrolments(db);
        var results = TestHelper.Results(db);

        var responses = new List<ActionResult>
        {
            auth.RefreshSession(),
            auth.Logout(),
            await profiles.GetParticipant(1),
            await profiles.UpdateParticipant(1, new UpdateProfileRequest()),
            await profiles.GetOrganiser(1),
            await profiles.UpdateOrganiser(1, new UpdateProfileRequest()),
            await events.GetEvents(),
            await events.GetEvent(1),
            await events.CreateEvent(new EventRequest()),
            await events.UpdateEvent(1, new EventRequest()),
            await events.DeleteEvent(1),
            await events.GetOrganiserEvents(1),
            await categories.GetEventCategories(1),
            await categories.GetCategory(1),
            await categories.CreateCategory(1, new CategoryRequest()),
            await categories.UpdateCategory(1, new CategoryRequest()),
            await categories.DeleteCategory(1),
            await enrolments.CreateEnrolment(new CreateEnrolmentRequest()),
            await enrolments.GetParticipantEnrolments(1),
            await enrolments.GetEnrolment(1),
            await enrolments.DeleteEnrolment(1),
            await enrolments.GetEventEnrolments(1),
            await enrolments.GetOrganiserEnrolments(1),
            await results.CaptureResult(new CaptureResultRequest()),
            await results.UpdateResult(1, new UpdateResultRequest()),
            await results.DeleteResult(1),
            await results.GetResult(1),
            await results.GetParticipantResults(1),
            await results.GetEventResults(1)
        };

        Assert.Equal(29, responses.Count);
        Assert.All(responses, r => Assert.Equal(401, TestHelper.StatusOf(r)));
    }

    [Fact]
    public async Task Participant_CallingOrganiserEndpoints_Returns403()
    {
        using var db = TestHelper.NewDb();
        var participant = TestHelper.AddParticipant(db);
        int id = participant.ParticipantId;
        var profiles = TestHelper.Profiles(db, id, Roles.Participant);
        var events = TestHelper.Events(db, id, Roles.Participant);
        var categories = TestHelper.Categories(db, id, Roles.Participant);
        var enrolments = TestHelper.Enrolments(db, id, Roles.Participant);
        var results = TestHelper.Results(db, id, Roles.Participant);

        var responses = new List<ActionResult>
        {
            await profiles.GetOrganiser(1),
            await profiles.UpdateOrganiser(1, new UpdateProfileRequest()),
            await events.CreateEvent(new EventRequest()),
            await events.UpdateEvent(1, new EventRequest()),
            await events.DeleteEvent(1),
            await events.GetOrganiserEvents(1),
            await categories.CreateCategory(1, new CategoryRequest()),
            await categories.UpdateCategory(1, new CategoryRequest()),
            await categories.DeleteCategory(1),
            await enrolments.GetEventEnrolments(1),
            await enrolments.GetOrganiserEnrolments(1),
            await results.CaptureResult(new CaptureResultRequest()),
            await results.UpdateResult(1, new UpdateResultRequest()),
            await results.DeleteResult(1),
            await results.GetEventResults(1)
        };

        Assert.All(responses, r => Assert.Equal(403, TestHelper.StatusOf(r)));
    }

    [Fact]
    public async Task Organiser_CallingParticipantOnlyEndpoints_Returns403()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        int id = organiser.OrganiserId;
        var profiles = TestHelper.Profiles(db, id, Roles.Organiser);
        var enrolments = TestHelper.Enrolments(db, id, Roles.Organiser);
        var results = TestHelper.Results(db, id, Roles.Organiser);

        var responses = new List<ActionResult>
        {
            await profiles.GetParticipant(1),
            await profiles.UpdateParticipant(1, new UpdateProfileRequest()),
            await enrolments.CreateEnrolment(new CreateEnrolmentRequest()),
            await enrolments.DeleteEnrolment(1),
            await enrolments.GetParticipantEnrolments(1),
            await results.GetParticipantResults(1)
        };

        Assert.All(responses, r => Assert.Equal(403, TestHelper.StatusOf(r)));
    }

    [Fact]
    public async Task BothRoles_CanBrowseEventsAndCategories()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var participant = TestHelper.AddParticipant(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var category = TestHelper.AddCategory(db, ev.EventId);

        var asOrganiserEvents = TestHelper.Events(db, organiser.OrganiserId, Roles.Organiser);
        var asOrganiserCategories = TestHelper.Categories(db, organiser.OrganiserId, Roles.Organiser);
        var asParticipantEvents = TestHelper.Events(db, participant.ParticipantId, Roles.Participant);
        var asParticipantCategories = TestHelper.Categories(db, participant.ParticipantId, Roles.Participant);

        var responses = new List<ActionResult>
        {
            await asOrganiserEvents.GetEvents(),
            await asOrganiserEvents.GetEvent(ev.EventId),
            await asOrganiserCategories.GetEventCategories(ev.EventId),
            await asOrganiserCategories.GetCategory(category.CategoryId),
            await asParticipantEvents.GetEvents(),
            await asParticipantEvents.GetEvent(ev.EventId),
            await asParticipantCategories.GetEventCategories(ev.EventId),
            await asParticipantCategories.GetCategory(category.CategoryId)
        };

        Assert.All(responses, r => Assert.Equal(200, TestHelper.StatusOf(r)));
    }
}