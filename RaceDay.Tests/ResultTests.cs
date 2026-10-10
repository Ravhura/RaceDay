using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCaptureResult_ForEnrolmentInOwnEvent()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);

        var response = await scenario.Organiser.Client.PostAsJsonAsync("/api/results",
            new { enrolmentId = scenario.EnrolmentId, finishTime = "01:58:10", position = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(scenario.EnrolmentId, json.GetProperty("enrolmentId").GetInt32());
        Assert.Equal(scenario.Participant.Id, json.GetProperty("participantId").GetInt32());
        Assert.Equal("01:58:10", json.GetProperty("finishTime").GetString());
        Assert.Equal(2, json.GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);

        var response = await scenario.Participant.Client.PostAsJsonAsync("/api/results",
            new { enrolmentId = scenario.EnrolmentId, finishTime = "00:30:00", position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResult_ForAnotherOrganisersEvent_Returns403()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var unrelated = await TestApi.NewOrganiserAsync(_factory);

        var response = await unrelated.Client.PostAsJsonAsync("/api/results",
            new { enrolmentId = scenario.EnrolmentId, finishTime = "00:30:00", position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_ForTheSameEnrolment_Returns409()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        await TestApi.CaptureResultAsync(scenario.Organiser.Client, scenario.EnrolmentId);

        var second = await scenario.Organiser.Client.PostAsJsonAsync("/api/results",
            new { enrolmentId = scenario.EnrolmentId, finishTime = "00:40:00", position = 3 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("00:30:00", 0)]      // position must be 1 or higher
    [InlineData("00:00:00", 1)]      // finish time must be greater than zero
    [InlineData("25:00:00", 1)]      // finish time must be under 24 hours (also not a valid time)
    public async Task CaptureResult_WithInvalidTimeOrPosition_Returns400(string finishTime, int position)
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);

        var response = await scenario.Organiser.Client.PostAsJsonAsync("/api/results",
            new { enrolmentId = scenario.EnrolmentId, finishTime, position });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForMissingEnrolment_Returns404()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);

        var response = await organiser.Client.PostAsJsonAsync("/api/results",
            new { enrolmentId = 999999, finishTime = "00:30:00", position = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewOwnResults()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var resultId = await TestApi.CaptureResultAsync(scenario.Organiser.Client, scenario.EnrolmentId);

        var list = await scenario.Participant.Client.GetFromJsonAsync<JsonElement>($"/api/participants/{scenario.Participant.Id}/results");
        Assert.Contains(list.EnumerateArray(), r => r.GetProperty("resultId").GetInt32() == resultId);

        var single = await scenario.Participant.Client.GetAsync($"/api/results/{resultId}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsResult_Returns403()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var resultId = await TestApi.CaptureResultAsync(scenario.Organiser.Client, scenario.EnrolmentId);
        var stranger = await TestApi.NewParticipantAsync(_factory);

        var single = await stranger.Client.GetAsync($"/api/results/{resultId}");
        var list = await stranger.Client.GetAsync($"/api/participants/{scenario.Participant.Id}/results");

        Assert.Equal(HttpStatusCode.Forbidden, single.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewUpdateAndDeleteResults_ForOwnEvent()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var resultId = await TestApi.CaptureResultAsync(scenario.Organiser.Client, scenario.EnrolmentId);

        var byEvent = await scenario.Organiser.Client.GetFromJsonAsync<JsonElement>($"/api/events/{scenario.EventId}/results");
        Assert.Contains(byEvent.EnumerateArray(), r => r.GetProperty("resultId").GetInt32() == resultId);

        var update = await scenario.Organiser.Client.PutAsJsonAsync($"/api/results/{resultId}",
            new { finishTime = "00:49:59", position = 1 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("position").GetInt32());

        var delete = await scenario.Organiser.Client.DeleteAsync($"/api/results/{resultId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateOrDeleteAnotherOrganisersResult_Returns403()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var resultId = await TestApi.CaptureResultAsync(scenario.Organiser.Client, scenario.EnrolmentId);
        var unrelated = await TestApi.NewOrganiserAsync(_factory);

        var update = await unrelated.Client.PutAsJsonAsync($"/api/results/{resultId}", new { finishTime = "00:10:00", position = 1 });
        var delete = await unrelated.Client.DeleteAsync($"/api/results/{resultId}");
        var byEvent = await unrelated.Client.GetAsync($"/api/events/{scenario.EventId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byEvent.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCancelEnrolment_OnceAResultExists_Returns409()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        await TestApi.CaptureResultAsync(scenario.Organiser.Client, scenario.EnrolmentId);

        var response = await scenario.Participant.Client.DeleteAsync($"/api/enrolments/{scenario.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}