using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

/// <summary>Organisers manage their own events. Other users are rejected.</summary>
public class EventTests
{
    [Fact]
    public async Task Organiser_CanCreateAnEvent_AndOwnsIt()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var controller = TestHelper.Events(db, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.CreateEvent(TestHelper.ValidEventRequest("Cape Town Cycle Tour"));

        Assert.Equal(201, TestHelper.StatusOf(result));
        var created = TestHelper.ValueOf<EventResponse>(result);
        Assert.Equal("Cape Town Cycle Tour", created.EventName);
        Assert.Equal(organiser.OrganiserId, created.OrganiserId); // the owner is the logged-in Organiser
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Participant_CannotCreateAnEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var participant = TestHelper.AddParticipant(db);
        var controller = TestHelper.Events(db, participant.ParticipantId, Roles.Participant);

        var result = await controller.CreateEvent(TestHelper.ValidEventRequest());

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task NotLoggedIn_CannotCreateAnEvent_Returns401()
    {
        using var db = TestHelper.NewDb();

        var result = await TestHelper.Events(db).CreateEvent(TestHelper.ValidEventRequest());

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Organiser_CannotCreateAnEventDatedInThePast_Returns400()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var controller = TestHelper.Events(db, organiser.OrganiserId, Roles.Organiser);
        var request = TestHelper.ValidEventRequest();
        request.EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3));

        var result = await controller.CreateEvent(request);

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = TestHelper.Events(db, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.UpdateEvent(ev.EventId, TestHelper.ValidEventRequest("Renamed Event"));

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Equal("Renamed Event", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddOrganiser(db, "owner@test.com");
        var other = TestHelper.AddOrganiser(db, "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.OrganiserId, "Original Name");
        var controller = TestHelper.Events(db, other.OrganiserId, Roles.Organiser);

        var result = await controller.UpdateEvent(ev.EventId, TestHelper.ValidEventRequest("Hijacked"));

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Equal("Original Name", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_AndItsCategories()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Events(db, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.DeleteEvent(ev.EventId);

        Assert.Equal(204, TestHelper.StatusOf(result));
        Assert.Empty(db.Events);
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddOrganiser(db, "owner@test.com");
        var other = TestHelper.AddOrganiser(db, "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.OrganiserId);
        var controller = TestHelper.Events(db, other.OrganiserId, Roles.Organiser);

        var result = await controller.DeleteEvent(ev.EventId);

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnEventThatHasEnrolments_Returns409()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var controller = TestHelper.Events(db, s.Organiser.OrganiserId, Roles.Organiser);

        var result = await controller.DeleteEvent(s.Event.EventId);

        Assert.Equal(409, TestHelper.StatusOf(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task GetEvent_WhenItDoesNotExist_Returns404()
    {
        using var db = TestHelper.NewDb();
        var participant = TestHelper.AddParticipant(db);

        var result = await TestHelper.Events(db, participant.ParticipantId, Roles.Participant).GetEvent(999);

        Assert.Equal(404, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Organiser_SeesOnlyTheirOwnEvents_AndCannotListAnotherOrganisersEvents()
    {
        using var db = TestHelper.NewDb();
        var a = TestHelper.AddOrganiser(db, "a@test.com");
        var b = TestHelper.AddOrganiser(db, "b@test.com");
        TestHelper.AddEvent(db, a.OrganiserId, "A's event");
        TestHelper.AddEvent(db, b.OrganiserId, "B's event");
        var controller = TestHelper.Events(db, a.OrganiserId, Roles.Organiser);

        var own = await controller.GetOrganiserEvents(a.OrganiserId);
        var other = await controller.GetOrganiserEvents(b.OrganiserId);

        Assert.Equal(200, TestHelper.StatusOf(own));
        var events = TestHelper.ValueOf<List<EventResponse>>(own);
        Assert.Single(events);
        Assert.Equal("A's event", events[0].EventName);
        Assert.Equal(403, TestHelper.StatusOf(other));
    }
}