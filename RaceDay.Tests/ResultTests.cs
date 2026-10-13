using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

/// <summary>Organisers capture results for their own events. Participants see only their own results.</summary>
public class ResultTests
{
    [Fact]
    public async Task Organiser_CanCaptureAResult_ForAnEnrolmentInTheirEvent()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var controller = TestHelper.Results(db, s.Organiser.OrganiserId, Roles.Organiser);

        var result = await controller.CaptureResult(new CaptureResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = new TimeSpan(1, 58, 10),
            Position = 2
        });

        Assert.Equal(201, TestHelper.StatusOf(result));
        var created = TestHelper.ValueOf<ResultResponse>(result);
        Assert.Equal(s.Participant.ParticipantId, created.ParticipantId);
        Assert.Equal(new TimeSpan(1, 58, 10), created.FinishTime);
        Assert.Equal(2, created.Position);
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task Participant_CannotCaptureAResult_Returns403()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var controller = TestHelper.Results(db, s.Participant.ParticipantId, Roles.Participant);

        var result = await controller.CaptureResult(new CaptureResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = new TimeSpan(0, 30, 0),
            Position = 1
        });

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Organiser_CannotCaptureAResult_ForAnotherOrganisersEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var unrelated = TestHelper.AddOrganiser(db, "unrelated@test.com");
        var controller = TestHelper.Results(db, unrelated.OrganiserId, Roles.Organiser);

        var result = await controller.CaptureResult(new CaptureResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = new TimeSpan(0, 30, 0),
            Position = 1
        });

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task CaptureResult_Twice_ForTheSameEnrolment_Returns409()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        TestHelper.AddResult(db, s.Enrolment.EnrolmentId);
        var controller = TestHelper.Results(db, s.Organiser.OrganiserId, Roles.Organiser);

        var result = await controller.CaptureResult(new CaptureResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = new TimeSpan(0, 40, 0),
            Position = 3
        });

        Assert.Equal(409, TestHelper.StatusOf(result));
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task CaptureResult_WithAnInvalidFinishTime_Returns400()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var controller = TestHelper.Results(db, s.Organiser.OrganiserId, Roles.Organiser);

        var zero = await controller.CaptureResult(new CaptureResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = TimeSpan.Zero,
            Position = 1
        });
        var tooLong = await controller.CaptureResult(new CaptureResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = TimeSpan.FromHours(25),
            Position = 1
        });

        Assert.Equal(400, TestHelper.StatusOf(zero));
        Assert.Equal(400, TestHelper.StatusOf(tooLong));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task CaptureResult_ForAMissingEnrolment_Returns404()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var controller = TestHelper.Results(db, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.CaptureResult(new CaptureResultRequest
        {
            EnrolmentId = 999,
            FinishTime = new TimeSpan(0, 30, 0),
            Position = 1
        });

        Assert.Equal(404, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Participant_CanViewOwnResults_ButNotAnothers()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var saved = TestHelper.AddResult(db, s.Enrolment.EnrolmentId);
        var stranger = TestHelper.AddParticipant(db, "stranger@test.com");

        var own = await TestHelper.Results(db, s.Participant.ParticipantId, Roles.Participant)
            .GetParticipantResults(s.Participant.ParticipantId);
        var single = await TestHelper.Results(db, s.Participant.ParticipantId, Roles.Participant)
            .GetResult(saved.ResultId);
        var notOwnList = await TestHelper.Results(db, stranger.ParticipantId, Roles.Participant)
            .GetParticipantResults(s.Participant.ParticipantId);
        var notOwnSingle = await TestHelper.Results(db, stranger.ParticipantId, Roles.Participant)
            .GetResult(saved.ResultId);

        Assert.Equal(200, TestHelper.StatusOf(own));
        Assert.Single(TestHelper.ValueOf<List<ResultResponse>>(own));
        Assert.Equal(200, TestHelper.StatusOf(single));
        Assert.Equal(403, TestHelper.StatusOf(notOwnList));
        Assert.Equal(403, TestHelper.StatusOf(notOwnSingle));
    }

    [Fact]
    public async Task Organiser_CanViewUpdateAndDeleteResults_ForTheirOwnEvent()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var saved = TestHelper.AddResult(db, s.Enrolment.EnrolmentId);
        var controller = TestHelper.Results(db, s.Organiser.OrganiserId, Roles.Organiser);

        var byEvent = await controller.GetEventResults(s.Event.EventId);
        Assert.Equal(200, TestHelper.StatusOf(byEvent));
        Assert.Single(TestHelper.ValueOf<List<ResultResponse>>(byEvent));

        var update = await controller.UpdateResult(saved.ResultId, new UpdateResultRequest { FinishTime = new TimeSpan(0, 49, 59), Position = 1 });
        Assert.Equal(200, TestHelper.StatusOf(update));
        Assert.Equal(1, TestHelper.ValueOf<ResultResponse>(update).Position);

        var delete = await controller.DeleteResult(saved.ResultId);
        Assert.Equal(204, TestHelper.StatusOf(delete));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Organiser_CannotUpdateDeleteOrListResults_OfAnotherOrganisersEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var saved = TestHelper.AddResult(db, s.Enrolment.EnrolmentId);
        var unrelated = TestHelper.AddOrganiser(db, "unrelated@test.com");
        var controller = TestHelper.Results(db, unrelated.OrganiserId, Roles.Organiser);

        var update = await controller.UpdateResult(saved.ResultId, new UpdateResultRequest { FinishTime = new TimeSpan(0, 10, 0), Position = 1 });
        var delete = await controller.DeleteResult(saved.ResultId);
        var byEvent = await controller.GetEventResults(s.Event.EventId);

        Assert.Equal(403, TestHelper.StatusOf(update));
        Assert.Equal(403, TestHelper.StatusOf(delete));
        Assert.Equal(403, TestHelper.StatusOf(byEvent));
        Assert.Single(db.Results);
    }
}