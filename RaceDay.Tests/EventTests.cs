using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

/// <summary>Organisers manage their own events; ownership and validation are enforced.</summary>
public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateEvent_AndFieldsAreStored()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);

        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Cape Town Cycle Tour",
            description = "A big cycle tour.",
            eventDate = DateTime.Today.AddDays(60).ToString("yyyy-MM-dd"),
            location = "Cape Town",
            distanceKm = 109.0,
            eventType = "Cycle"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cape Town Cycle Tour", json.GetProperty("eventName").GetString());
        Assert.Equal("Cycle", json.GetProperty("eventType").GetString());
        Assert.Equal(organiser.Id, json.GetProperty("organiserId").GetInt32()); // owner = the logged-in Organiser
        Assert.Equal("Upcoming", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await TestApi.NewParticipantAsync(_factory);
        var response = await participant.Client.PostAsJsonAsync("/api/events", TestApi.ValidEvent());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", TestApi.ValidEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Renamed Event",
            description = "Updated description.",
            eventDate = DateTime.Today.AddDays(45).ToString("yyyy-MM-dd"),
            location = "Pretoria",
            distanceKm = 21.1,
            eventType = "Walk"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Renamed Event", json.GetProperty("eventName").GetString());
        Assert.Equal("Walk", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestApi.NewOrganiserAsync(_factory);
        var other = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(owner.Client);

        var response = await other.Client.PutAsJsonAsync($"/api/events/{eventId}", TestApi.ValidEvent("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestApi.NewOrganiserAsync(_factory);
        var other = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(owner.Client);

        var response = await other.Client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_WithItsCategories()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);
        await TestApi.CreateCategoryAsync(organiser.Client, eventId);

        var delete = await organiser.Client.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await organiser.Client.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteEventThatHasEnrolments_Returns409()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);

        var response = await scenario.Organiser.Client.DeleteAsync($"/api/events/{scenario.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_WhenMissing_Returns404()
    {
        var participant = await TestApi.NewParticipantAsync(_factory);
        var response = await participant.Client.GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ListsOnlyTheirOwnEvents_AndCannotListAnotherOrganisersEvents()
    {
        var a = await TestApi.NewOrganiserAsync(_factory);
        var b = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(a.Client);

        var own = await a.Client.GetFromJsonAsync<JsonElement>($"/api/organisers/{a.Id}/events");
        Assert.Contains(own.EnumerateArray(), e => e.GetProperty("eventId").GetInt32() == eventId);

        var other = await b.Client.GetAsync($"/api/organisers/{a.Id}/events");
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithUnknownEventType_Returns400()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Swim Event",
            description = "x",
            eventDate = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd"),
            location = "Durban",
            distanceKm = 1.5,
            eventType = "Swim"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingEventType_Returns400()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventName = "No Type",
            description = "x",
            eventDate = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd"),
            location = "Durban",
            distanceKm = 5.0
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithZeroDistance_Returns400()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Zero",
            description = "x",
            eventDate = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd"),
            location = "Durban",
            distanceKm = 0,
            eventType = "Run"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_Returns400()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Past",
            description = "x",
            eventDate = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd"),
            location = "Durban",
            distanceKm = 5.0,
            eventType = "Run"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}