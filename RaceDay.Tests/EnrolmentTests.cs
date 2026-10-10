using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_AndTheEnrolmentRecordsParticipantEventAndCategory()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var participant = await TestApi.NewParticipantAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);
        var categoryId = await TestApi.CreateCategoryAsync(organiser.Client, eventId);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(participant.Id, json.GetProperty("participantId").GetInt32()); // from the session
        Assert.Equal(eventId, json.GetProperty("eventId").GetInt32());
        Assert.Equal(categoryId, json.GetProperty("categoryId").GetInt32());
        Assert.Equal("Confirmed", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);
        var categoryId = await TestApi.CreateCategoryAsync(organiser.Client, eventId);

        var response = await organiser.Client.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromADifferentEvent_Returns400()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var participant = await TestApi.NewParticipantAsync(_factory);
        var eventA = await TestApi.CreateEventAsync(organiser.Client);
        var eventB = await TestApi.CreateEventAsync(organiser.Client);
        var categoryOfB = await TestApi.CreateCategoryAsync(organiser.Client, eventB);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments",
            new { eventId = eventA, categoryId = categoryOfB });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_InTheSameEventAndCategory_Returns409()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);

        var second = await scenario.Participant.Client.PostAsJsonAsync("/api/enrolments",
            new { eventId = scenario.EventId, categoryId = scenario.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingEventOrCategory_Returns404()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var participant = await TestApi.NewParticipantAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);

        var missingEvent = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId = 999999, categoryId = 1 });
        var missingCategory = await participant.Client.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, missingEvent.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingCategory.StatusCode);
    }

    [Fact]
    public async Task Participant_CanListOwnEnrolments_ButNotAnothersList()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var stranger = await TestApi.NewParticipantAsync(_factory);

        var own = await scenario.Participant.Client.GetFromJsonAsync<JsonElement>($"/api/participants/{scenario.Participant.Id}/enrolments");
        Assert.Contains(own.EnumerateArray(), e => e.GetProperty("enrolmentId").GetInt32() == scenario.EnrolmentId);

        var other = await stranger.Client.GetAsync($"/api/participants/{scenario.Participant.Id}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsEnrolment_Returns403()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var stranger = await TestApi.NewParticipantAsync(_factory);

        var owner = await scenario.Participant.Client.GetAsync($"/api/enrolments/{scenario.EnrolmentId}");
        var other = await stranger.Client.GetAsync($"/api/enrolments/{scenario.EnrolmentId}");

        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent_AndSeesTheParticipant()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);

        var byEvent = await scenario.Organiser.Client.GetFromJsonAsync<JsonElement>($"/api/events/{scenario.EventId}/enrolments");
        Assert.Contains(byEvent.EnumerateArray(), e =>
            e.GetProperty("enrolmentId").GetInt32() == scenario.EnrolmentId &&
            e.GetProperty("participantId").GetInt32() == scenario.Participant.Id);

        var byOrganiser = await scenario.Organiser.Client.GetFromJsonAsync<JsonElement>($"/api/organisers/{scenario.Organiser.Id}/enrolments");
        Assert.Contains(byOrganiser.EnumerateArray(), e => e.GetProperty("enrolmentId").GetInt32() == scenario.EnrolmentId);

        var single = await scenario.Organiser.Client.GetAsync($"/api/enrolments/{scenario.EnrolmentId}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnUnrelatedEvent_Returns403()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var unrelated = await TestApi.NewOrganiserAsync(_factory);

        var byEvent = await unrelated.Client.GetAsync($"/api/events/{scenario.EventId}/enrolments");
        var single = await unrelated.Client.GetAsync($"/api/enrolments/{scenario.EnrolmentId}");
        var byOrganiser = await unrelated.Client.GetAsync($"/api/organisers/{scenario.Organiser.Id}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, byEvent.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, single.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byOrganiser.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_ButNotAnothers()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);
        var stranger = await TestApi.NewParticipantAsync(_factory);

        var byStranger = await stranger.Client.DeleteAsync($"/api/enrolments/{scenario.EnrolmentId}");
        Assert.Equal(HttpStatusCode.Forbidden, byStranger.StatusCode);

        var byOwner = await scenario.Participant.Client.DeleteAsync($"/api/enrolments/{scenario.EnrolmentId}");
        Assert.Equal(HttpStatusCode.NoContent, byOwner.StatusCode);
    }

    [Fact]
    public async Task Database_RejectsAnEnrolmentWhoseCategoryBelongsToAnotherEvent()
    {
        // Proves the Part 1 composite foreign key (EventId, CategoryId) works even if the service check is bypassed.
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var participant = await TestApi.NewParticipantAsync(_factory);
        var eventA = await TestApi.CreateEventAsync(organiser.Client);
        var eventB = await TestApi.CreateEventAsync(organiser.Client);
        var categoryOfB = await TestApi.CreateCategoryAsync(organiser.Client, eventB);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        db.Enrolments.Add(new Enrolment { ParticipantId = participant.Id, EventId = eventA, CategoryId = categoryOfB });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}