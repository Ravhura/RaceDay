using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

/// <summary>A registered, logged-in test user. Each user has their own HttpClient (and so their own session cookie).</summary>
public record TestUser(HttpClient Client, int Id, string Email);

/// <summary>Helpers that call the real API so tests stay short and readable.</summary>
public static class TestApi
{
    public const string Password = "Passw0rd123";

    public static string NewEmail() => $"{Guid.NewGuid():N}@test.raceday.local";

    public static async Task<TestUser> NewParticipantAsync(RaceDayFactory factory)
    {
        var client = factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register/participant",
            new { fullName = "Test Participant", email, password = Password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var id = await ReadIdAsync(register, "participantId");

        var login = await client.PostAsJsonAsync("/api/auth/login/participant", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return new TestUser(client, id, email);
    }

    public static async Task<TestUser> NewOrganiserAsync(RaceDayFactory factory)
    {
        var client = factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register/organiser",
            new { fullName = "Test Organiser", email, password = Password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var id = await ReadIdAsync(register, "organiserId");

        var login = await client.PostAsJsonAsync("/api/auth/login/organiser", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return new TestUser(client, id, email);
    }

    /// <summary>A valid event request body. Override fields by building your own anonymous object when needed.</summary>
    public static object ValidEvent(string name = "Test 10km Run") => new
    {
        eventName = name,
        description = "A test event.",
        eventDate = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"),
        location = "Johannesburg",
        distanceKm = 10.0,
        eventType = "Run"
    };

    public static async Task<int> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadIdAsync(response, "eventId");
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryName = "10km", distanceKm = 10.0, entryFee = 150.0 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadIdAsync(response, "categoryId");
    }

    public static async Task<int> EnrolAsync(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync("/api/enrolments", new { eventId, categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadIdAsync(response, "enrolmentId");
    }

    public static async Task<int> CaptureResultAsync(HttpClient organiser, int enrolmentId)
    {
        var response = await organiser.PostAsJsonAsync("/api/results",
            new { enrolmentId, finishTime = "00:52:30", position = 5 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadIdAsync(response, "resultId");
    }

    /// <summary>Creates organiser + participant + event + category + enrolment in one go.</summary>
    public static async Task<(TestUser Organiser, TestUser Participant, int EventId, int CategoryId, int EnrolmentId)>
        NewEnrolmentScenarioAsync(RaceDayFactory factory)
    {
        var organiser = await NewOrganiserAsync(factory);
        var participant = await NewParticipantAsync(factory);
        var eventId = await CreateEventAsync(organiser.Client);
        var categoryId = await CreateCategoryAsync(organiser.Client, eventId);
        var enrolmentId = await EnrolAsync(participant.Client, eventId, categoryId);
        return (organiser, participant, eventId, categoryId, enrolmentId);
    }

    public static async Task<int> ReadIdAsync(HttpResponseMessage response, string property)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(property).GetInt32();
    }
}