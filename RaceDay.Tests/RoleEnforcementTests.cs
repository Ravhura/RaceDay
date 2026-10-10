using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

/// <summary>Participants cannot use Organiser endpoints, and Organisers cannot use Participant-only endpoints.</summary>
public class RoleEnforcementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleEnforcementTests(RaceDayFactory factory) => _factory = factory;

    private static HttpRequestMessage Build(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }
        return request;
    }

    [Theory]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("GET", "/api/organisers/1/events")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("GET", "/api/events/1/enrolments")]
    [InlineData("GET", "/api/organisers/1/enrolments")]
    [InlineData("POST", "/api/results")]
    [InlineData("PUT", "/api/results/1")]
    [InlineData("DELETE", "/api/results/1")]
    [InlineData("GET", "/api/events/1/results")]
    [InlineData("GET", "/api/organisers/1")]
    [InlineData("PUT", "/api/organisers/1")]
    public async Task Participant_CallingOrganiserEndpoint_Returns403(string method, string url)
    {
        var participant = await TestApi.NewParticipantAsync(_factory);

        var response = await participant.Client.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/enrolments")]
    [InlineData("DELETE", "/api/enrolments/1")]
    [InlineData("GET", "/api/participants/1/enrolments")]
    [InlineData("GET", "/api/participants/1/results")]
    [InlineData("GET", "/api/participants/1")]
    [InlineData("PUT", "/api/participants/1")]
    public async Task Organiser_CallingParticipantOnlyEndpoint_Returns403(string method, string url)
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);

        var response = await organiser.Client.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanBrowseEventsAndCategories()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var participant = await TestApi.NewParticipantAsync(_factory);
        var eventId = await TestApi.CreateEventAsync(organiser.Client);
        await TestApi.CreateCategoryAsync(organiser.Client, eventId);

        foreach (var client in new[] { organiser.Client, participant.Client })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/events")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/events/{eventId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/events/{eventId}/categories")).StatusCode);
        }
    }
}