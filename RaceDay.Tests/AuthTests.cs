using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Authentication;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Registration, login, password hashing, sessions and profiles.</summary>
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    // ---------- Registration ----------

    [Fact]
    public async Task RegisterParticipant_WithValidDetails_Returns201AndNewId()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register/participant",
            new { fullName = "Jane Runner", email = TestApi.NewEmail(), password = TestApi.Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(await TestApi.ReadIdAsync(response, "participantId") > 0);
    }

    [Fact]
    public async Task RegisterOrganiser_WithValidDetails_Returns201AndNewId()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register/organiser",
            new { fullName = "Olivia Organiser", email = TestApi.NewEmail(), password = TestApi.Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(await TestApi.ReadIdAsync(response, "organiserId") > 0);
    }

    [Fact]
    public async Task RegisterParticipant_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var body = new { fullName = "Jane Runner", email = TestApi.NewEmail(), password = TestApi.Password };

        await client.PostAsJsonAsync("/api/auth/register/participant", body);
        var second = await client.PostAsJsonAsync("/api/auth/register/participant", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RegisterParticipant_InvalidEmail_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register/participant",
            new { fullName = "Jane Runner", email = "not-an-email", password = TestApi.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("short1")]            // too short
    [InlineData("onlyletters")]       // no digit
    [InlineData("12345678")]          // no letter
    public async Task RegisterParticipant_WeakPassword_Returns400(string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register/participant",
            new { fullName = "Jane Runner", email = TestApi.NewEmail(), password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterParticipant_MissingName_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register/participant",
            new { email = TestApi.NewEmail(), password = TestApi.Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Login ----------

    [Fact]
    public async Task LoginParticipant_WithValidCredentials_Returns200AndParticipantRole()
    {
        var user = await TestApi.NewParticipantAsync(_factory);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login/participant",
            new { email = user.Email, password = TestApi.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Participant", json.GetProperty("role").GetString());
        Assert.Equal(user.Id, json.GetProperty("userId").GetInt32());
    }

    [Fact]
    public async Task LoginOrganiser_WithValidCredentials_Returns200AndOrganiserRole()
    {
        var user = await TestApi.NewOrganiserAsync(_factory);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login/organiser",
            new { email = user.Email, password = TestApi.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Organiser", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task LoginParticipant_WrongPassword_Returns401()
    {
        var user = await TestApi.NewParticipantAsync(_factory);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login/participant",
            new { email = user.Email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginParticipant_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login/participant",
            new { email = TestApi.NewEmail(), password = TestApi.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginOrganiserRoute_WithParticipantAccount_Returns401()
    {
        // A Participant account cannot log in through the Organiser route.
        var participant = await TestApi.NewParticipantAsync(_factory);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login/organiser",
            new { email = participant.Email, password = TestApi.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Password hashing ----------

    [Fact]
    public async Task Password_IsHashed_AndVerifiesCorrectly()
    {
        var user = await TestApi.NewParticipantAsync(_factory);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordService>();

        var stored = await db.Participants.SingleAsync(p => p.Email == user.Email);

        Assert.NotEqual(TestApi.Password, stored.PasswordHash);          // not plaintext
        Assert.DoesNotContain(TestApi.Password, stored.PasswordHash);
        Assert.True(hasher.Verify(TestApi.Password, stored.PasswordHash));   // right password verifies
        Assert.False(hasher.Verify("WrongPassword1", stored.PasswordHash));  // wrong password fails
    }

    [Fact]
    public void PasswordService_HashingTwiceGivesDifferentHashes_BecauseOfSalt()
    {
        var hasher = new PasswordService();
        Assert.NotEqual(hasher.Hash(TestApi.Password), hasher.Hash(TestApi.Password));
    }

    // ---------- Sessions ----------

    [Theory]
    [InlineData("GET", "/api/events")]
    [InlineData("GET", "/api/events/1")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("GET", "/api/organisers/1/events")]
    [InlineData("GET", "/api/events/1/categories")]
    [InlineData("GET", "/api/categories/1")]
    [InlineData("POST", "/api/events/1/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    [InlineData("POST", "/api/enrolments")]
    [InlineData("GET", "/api/enrolments/1")]
    [InlineData("DELETE", "/api/enrolments/1")]
    [InlineData("GET", "/api/participants/1/enrolments")]
    [InlineData("GET", "/api/events/1/enrolments")]
    [InlineData("GET", "/api/organisers/1/enrolments")]
    [InlineData("POST", "/api/results")]
    [InlineData("PUT", "/api/results/1")]
    [InlineData("DELETE", "/api/results/1")]
    [InlineData("GET", "/api/results/1")]
    [InlineData("GET", "/api/participants/1/results")]
    [InlineData("GET", "/api/events/1/results")]
    [InlineData("GET", "/api/participants/1")]
    [InlineData("PUT", "/api/participants/1")]
    [InlineData("GET", "/api/organisers/1")]
    [InlineData("PUT", "/api/organisers/1")]
    [InlineData("POST", "/api/auth/logout")]
    [InlineData("POST", "/api/auth/refresh-token")]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401(string method, string url)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var user = await TestApi.NewParticipantAsync(_factory);
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync("/api/events")).StatusCode);

        var logout = await user.Client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await user.Client.GetAsync("/api/events")).StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithActiveSession_Returns200()
    {
        var user = await TestApi.NewParticipantAsync(_factory);
        var response = await user.Client.PostAsync("/api/auth/refresh-token", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoggingInAsAnotherRole_ReplacesTheSession()
    {
        var organiser = await TestApi.NewOrganiserAsync(_factory);
        var participant = await TestApi.NewParticipantAsync(_factory);

        // Use the organiser's client to log in as a participant: the role must switch.
        var client = organiser.Client;
        await client.PostAsJsonAsync("/api/auth/login/participant", new { email = participant.Email, password = TestApi.Password });

        var asParticipant = await client.GetAsync($"/api/participants/{participant.Id}");
        Assert.Equal(HttpStatusCode.OK, asParticipant.StatusCode);

        var asOrganiser = await client.GetAsync($"/api/organisers/{organiser.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, asOrganiser.StatusCode);
    }

    // ---------- Profiles ----------

    [Fact]
    public async Task Participant_CanViewOwnProfile_WithoutPasswordHash()
    {
        var user = await TestApi.NewParticipantAsync(_factory);
        var response = await user.Client.GetAsync($"/api/participants/{user.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(user.Email, body);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsProfile_Returns403()
    {
        var a = await TestApi.NewParticipantAsync(_factory);
        var b = await TestApi.NewParticipantAsync(_factory);

        var response = await a.Client.GetAsync($"/api/participants/{b.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanUpdateOwnProfile_ButNotAnothers()
    {
        var a = await TestApi.NewParticipantAsync(_factory);
        var b = await TestApi.NewParticipantAsync(_factory);

        var own = await a.Client.PutAsJsonAsync($"/api/participants/{a.Id}",
            new { fullName = "Updated Name", phoneNumber = "0821234567" });
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Contains("Updated Name", await own.Content.ReadAsStringAsync());

        var other = await a.Client.PutAsJsonAsync($"/api/participants/{b.Id}",
            new { fullName = "Hacked", phoneNumber = "0821234567" });
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewAndUpdateOwnProfile_ButNotAnothers()
    {
        var a = await TestApi.NewOrganiserAsync(_factory);
        var b = await TestApi.NewOrganiserAsync(_factory);

        Assert.Equal(HttpStatusCode.OK, (await a.Client.GetAsync($"/api/organisers/{a.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.Client.GetAsync($"/api/organisers/{b.Id}")).StatusCode);

        var update = await a.Client.PutAsJsonAsync($"/api/organisers/{a.Id}",
            new { fullName = "Updated Organiser", phoneNumber = "0831234567" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidPhone_Returns400()
    {
        var user = await TestApi.NewParticipantAsync(_factory);
        var response = await user.Client.PutAsJsonAsync($"/api/participants/{user.Id}",
            new { fullName = "Name", phoneNumber = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}