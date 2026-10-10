using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class CategoryTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public CategoryTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateCategory_ForOwnEvent()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Under 20", entryFee = 80.0 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(eventId, json.GetProperty("eventId").GetInt32());
        Assert.Equal("Under 20", json.GetProperty("categoryName").GetString());
    }

    [Fact]
    public async Task Organiser_CannotCreateCategory_ForAnotherOrganisersEvent_Returns403()
    {
        var owner = await TestApi.NewOrganiserAsync(_factory);
        var other = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(owner.Client);

        var response = await other.Client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Sneaky", entryFee = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var participant = await TestApi.NewParticipantAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);

        var response = await participant.Client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Nope", entryFee = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnCategory()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);
        var categoryId = await TestApi.CreateCategoryAsync(organiser.Client, eventId);

        var update = await organiser.Client.PutAsJsonAsync($"/api/categories/{categoryId}",
            new { categoryName = "Senior", distanceKm = 21.1, entryFee = 200.0 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await organiser.Client.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await organiser.Client.GetAsync($"/api/categories/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateOrDeleteAnotherOrganisersCategory_Returns403()
    {
        var owner = await TestApi.NewOrganiserAsync(_factory);
        var other = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(owner.Client);
        var categoryId = await TestApi.CreateCategoryAsync(owner.Client, eventId);

        var update = await other.Client.PutAsJsonAsync($"/api/categories/{categoryId}",
            new { categoryName = "Hijacked", entryFee = 0 });
        var delete = await other.Client.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteCategoryThatHasEnrolments_Returns409()
    {
        var scenario = await TestApi.NewEnrolmentScenarioAsync(_factory);

        var response = await scenario.Organiser.Client.DeleteAsync($"/api/categories/{scenario.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithNegativeFee_Returns400()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);

        var response = await organiser.Client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "Bad Fee", entryFee = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListCategories_ForMissingEvent_Returns404()
    {
        var participant = await TestApi.NewParticipantAsync(_factory);
        var response = await participant.Client.GetAsync("/api/events/999999/categories");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}