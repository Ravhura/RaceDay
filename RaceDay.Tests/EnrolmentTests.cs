using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

/// <summary>Participants enrol. The enrolment is recorded correctly, and bad requests are rejected.</summary>
public class EnrolmentTests
{
    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentRecordsParticipantEventAndCategory()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var participant = TestHelper.AddParticipant(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Enrolments(db, participant.ParticipantId, Roles.Participant);

        var result = await controller.CreateEnrolment(new CreateEnrolmentRequest { EventId = ev.EventId, CategoryId = category.CategoryId });

        Assert.Equal(201, TestHelper.StatusOf(result));
        var saved = Assert.Single(db.Enrolments);
        Assert.Equal(participant.ParticipantId, saved.ParticipantId); // taken from the session
        Assert.Equal(ev.EventId, saved.EventId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal("Confirmed", saved.Status);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Enrolments(db, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.CreateEnrolment(new CreateEnrolmentRequest { EventId = ev.EventId, CategoryId = category.CategoryId });

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_WithACategoryFromADifferentEvent_Returns400()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var participant = TestHelper.AddParticipant(db);
        var eventA = TestHelper.AddEvent(db, organiser.OrganiserId, "Event A");
        var eventB = TestHelper.AddEvent(db, organiser.OrganiserId, "Event B");
        var categoryOfB = TestHelper.AddCategory(db, eventB.EventId);
        var controller = TestHelper.Enrolments(db, participant.ParticipantId, Roles.Participant);

        var result = await controller.CreateEnrolment(new CreateEnrolmentRequest { EventId = eventA.EventId, CategoryId = categoryOfB.CategoryId });

        Assert.Equal(400, TestHelper.StatusOf(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_Twice_InTheSameEventAndCategory_Returns409()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var controller = TestHelper.Enrolments(db, s.Participant.ParticipantId, Roles.Participant);

        var second = await controller.CreateEnrolment(new CreateEnrolmentRequest { EventId = s.Event.EventId, CategoryId = s.Category.CategoryId });

        Assert.Equal(409, TestHelper.StatusOf(second));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_InAMissingEventOrCategory_Returns404()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var participant = TestHelper.AddParticipant(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Enrolments(db, participant.ParticipantId, Roles.Participant);

        var missingEvent = await controller.CreateEnrolment(new CreateEnrolmentRequest { EventId = 999, CategoryId = category.CategoryId });
        var missingCategory = await controller.CreateEnrolment(new CreateEnrolmentRequest { EventId = ev.EventId, CategoryId = 999 });

        Assert.Equal(404, TestHelper.StatusOf(missingEvent));
        Assert.Equal(404, TestHelper.StatusOf(missingCategory));
    }

    [Fact]
    public async Task Participant_CanListOwnEnrolments_ButNotAnothersList()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var stranger = TestHelper.AddParticipant(db, "stranger@test.com");

        var own = await TestHelper.Enrolments(db, s.Participant.ParticipantId, Roles.Participant)
            .GetParticipantEnrolments(s.Participant.ParticipantId);
        var notOwn = await TestHelper.Enrolments(db, stranger.ParticipantId, Roles.Participant)
            .GetParticipantEnrolments(s.Participant.ParticipantId);

        Assert.Equal(200, TestHelper.StatusOf(own));
        var list = TestHelper.ValueOf<List<EnrolmentResponse>>(own);
        Assert.Single(list);
        Assert.Equal(s.Enrolment.EnrolmentId, list[0].EnrolmentId);
        Assert.Equal(403, TestHelper.StatusOf(notOwn));
    }

    [Fact]
    public async Task OnlyTheOwnerOrTheManagingOrganiser_CanViewAnEnrolment()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var strangerParticipant = TestHelper.AddParticipant(db, "stranger@test.com");
        var unrelatedOrganiser = TestHelper.AddOrganiser(db, "unrelated@test.com");
        int id = s.Enrolment.EnrolmentId;

        var byOwner = await TestHelper.Enrolments(db, s.Participant.ParticipantId, Roles.Participant).GetEnrolment(id);
        var byOrganiser = await TestHelper.Enrolments(db, s.Organiser.OrganiserId, Roles.Organiser).GetEnrolment(id);
        var byStranger = await TestHelper.Enrolments(db, strangerParticipant.ParticipantId, Roles.Participant).GetEnrolment(id);
        var byUnrelated = await TestHelper.Enrolments(db, unrelatedOrganiser.OrganiserId, Roles.Organiser).GetEnrolment(id);

        Assert.Equal(200, TestHelper.StatusOf(byOwner));
        Assert.Equal(200, TestHelper.StatusOf(byOrganiser));
        Assert.Equal(403, TestHelper.StatusOf(byStranger));
        Assert.Equal(403, TestHelper.StatusOf(byUnrelated));
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent_AndSeesTheParticipant()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var controller = TestHelper.Enrolments(db, s.Organiser.OrganiserId, Roles.Organiser);

        var byEvent = await controller.GetEventEnrolments(s.Event.EventId);
        var byOrganiser = await controller.GetOrganiserEnrolments(s.Organiser.OrganiserId);

        Assert.Equal(200, TestHelper.StatusOf(byEvent));
        var list = TestHelper.ValueOf<List<EnrolmentResponse>>(byEvent);
        Assert.Single(list);
        Assert.Equal(s.Participant.ParticipantId, list[0].ParticipantId);
        Assert.Equal(200, TestHelper.StatusOf(byOrganiser));
        Assert.Single(TestHelper.ValueOf<List<EnrolmentResponse>>(byOrganiser));
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnUnrelatedEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var unrelated = TestHelper.AddOrganiser(db, "unrelated@test.com");
        var controller = TestHelper.Enrolments(db, unrelated.OrganiserId, Roles.Organiser);

        var byEvent = await controller.GetEventEnrolments(s.Event.EventId);
        var byOrganiser = await controller.GetOrganiserEnrolments(s.Organiser.OrganiserId);

        Assert.Equal(403, TestHelper.StatusOf(byEvent));
        Assert.Equal(403, TestHelper.StatusOf(byOrganiser));
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_ButNotAnothers()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var stranger = TestHelper.AddParticipant(db, "stranger@test.com");

        var byStranger = await TestHelper.Enrolments(db, stranger.ParticipantId, Roles.Participant).DeleteEnrolment(s.Enrolment.EnrolmentId);
        Assert.Equal(403, TestHelper.StatusOf(byStranger));
        Assert.Single(db.Enrolments);

        var byOwner = await TestHelper.Enrolments(db, s.Participant.ParticipantId, Roles.Participant).DeleteEnrolment(s.Enrolment.EnrolmentId);
        Assert.Equal(204, TestHelper.StatusOf(byOwner));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Participant_CannotCancelAnEnrolment_OnceAResultExists_Returns409()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        TestHelper.AddResult(db, s.Enrolment.EnrolmentId);
        var controller = TestHelper.Enrolments(db, s.Participant.ParticipantId, Roles.Participant);

        var result = await controller.DeleteEnrolment(s.Enrolment.EnrolmentId);

        Assert.Equal(409, TestHelper.StatusOf(result));
        Assert.Single(db.Enrolments);
    }
}