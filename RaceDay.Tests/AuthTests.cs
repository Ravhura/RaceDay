using Microsoft.AspNetCore.Http;
using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>Registration, login, password hashing, sessions and profiles.</summary>
public class AuthTests
{
    private static RegisterRequest NewRegisterRequest(string email) =>
        new RegisterRequest { FullName = "Test User", Email = email, Password = TestHelper.Password };

    // ---------- Registration ----------

    [Fact]
    public async Task RegisterParticipant_WithValidDetails_Returns201()
    {
        using var db = TestHelper.NewDb();

        var result = await TestHelper.Auth(db).RegisterParticipant(NewRegisterRequest("jane@test.com"));

        Assert.Equal(201, TestHelper.StatusOf(result));
        Assert.True(TestHelper.ValueOf<ParticipantCreatedResponse>(result).ParticipantId > 0);
    }

    [Fact]
    public async Task RegisterOrganiser_WithValidDetails_Returns201()
    {
        using var db = TestHelper.NewDb();

        var result = await TestHelper.Auth(db).RegisterOrganiser(NewRegisterRequest("olivia@test.com"));

        Assert.Equal(201, TestHelper.StatusOf(result));
        Assert.True(TestHelper.ValueOf<OrganiserCreatedResponse>(result).OrganiserId > 0);
    }

    [Fact]
    public async Task RegisterParticipant_DuplicateEmail_Returns409()
    {
        using var db = TestHelper.NewDb();
        var controller = TestHelper.Auth(db);

        await controller.RegisterParticipant(NewRegisterRequest("jane@test.com"));
        var second = await controller.RegisterParticipant(NewRegisterRequest("jane@test.com"));

        Assert.Equal(409, TestHelper.StatusOf(second));
    }

    [Fact]
    public async Task Register_StoresAHashedPassword_NotPlainText()
    {
        using var db = TestHelper.NewDb();

        await TestHelper.Auth(db).RegisterParticipant(NewRegisterRequest("jane@test.com"));

        var saved = db.Participants.Single(p => p.Email == "jane@test.com");
        Assert.NotEqual(TestHelper.Password, saved.PasswordHash);                       // not plain text
        Assert.True(PasswordHelper.Verify(TestHelper.Password, saved.PasswordHash));    // right password matches
        Assert.False(PasswordHelper.Verify("WrongPassword1", saved.PasswordHash));      // wrong password does not
    }

    // ---------- Login and session ----------

    [Fact]
    public async Task LoginParticipant_WithValidCredentials_Returns200AndStartsSession()
    {
        using var db = TestHelper.NewDb();
        var participant = TestHelper.AddParticipant(db, "jane@test.com");
        var controller = TestHelper.Auth(db);

        var result = await controller.LoginParticipant(new LoginRequest { Email = "jane@test.com", Password = TestHelper.Password });

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Equal(Roles.Participant, TestHelper.ValueOf<LoginResponse>(result).Role);
        // The server-side session now holds the user's id and role.
        Assert.Equal(participant.ParticipantId, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal(Roles.Participant, controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task LoginOrganiser_WithValidCredentials_Returns200AndStartsSession()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db, "olivia@test.com");
        var controller = TestHelper.Auth(db);

        var result = await controller.LoginOrganiser(new LoginRequest { Email = "olivia@test.com", Password = TestHelper.Password });

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Equal(organiser.OrganiserId, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal(Roles.Organiser, controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_IgnoresUpperAndLowerCaseInTheEmail()
    {
        using var db = TestHelper.NewDb();
        TestHelper.AddParticipant(db, "jane@test.com");

        var result = await TestHelper.Auth(db).LoginParticipant(new LoginRequest { Email = "JANE@Test.com", Password = TestHelper.Password });

        Assert.Equal(200, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task LoginParticipant_WrongPassword_Returns401AndNoSession()
    {
        using var db = TestHelper.NewDb();
        TestHelper.AddParticipant(db, "jane@test.com");
        var controller = TestHelper.Auth(db);

        var result = await controller.LoginParticipant(new LoginRequest { Email = "jane@test.com", Password = "WrongPassword1" });

        Assert.Equal(401, TestHelper.StatusOf(result));
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task LoginParticipant_UnknownEmail_Returns401()
    {
        using var db = TestHelper.NewDb();

        var result = await TestHelper.Auth(db).LoginParticipant(new LoginRequest { Email = "nobody@test.com", Password = TestHelper.Password });

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task LoginOrganiserRoute_WithAParticipantAccount_Returns401()
    {
        using var db = TestHelper.NewDb();
        TestHelper.AddParticipant(db, "jane@test.com");

        var result = await TestHelper.Auth(db).LoginOrganiser(new LoginRequest { Email = "jane@test.com", Password = TestHelper.Password });

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Logout_ClearsTheSession()
    {
        using var db = TestHelper.NewDb();
        TestHelper.AddParticipant(db, "jane@test.com");
        var controller = TestHelper.Auth(db);
        await controller.LoginParticipant(new LoginRequest { Email = "jane@test.com", Password = TestHelper.Password });

        var result = controller.Logout();

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.Role));
    }

    [Fact]
    public void Logout_WhenNotLoggedIn_Returns401()
    {
        using var db = TestHelper.NewDb();

        var result = TestHelper.Auth(db).Logout();

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public void RefreshSession_WhenLoggedIn_Returns200()
    {
        using var db = TestHelper.NewDb();

        var result = TestHelper.Auth(db, 1, Roles.Participant).RefreshSession();

        Assert.Equal(200, TestHelper.StatusOf(result));
    }

    [Fact]
    public void RefreshSession_WhenNotLoggedIn_Returns401()
    {
        using var db = TestHelper.NewDb();

        var result = TestHelper.Auth(db).RefreshSession();

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    // ---------- Profiles ----------

    [Fact]
    public async Task Participant_CanViewOwnProfile_WithoutAPasswordHash()
    {
        using var db = TestHelper.NewDb();
        var participant = TestHelper.AddParticipant(db);
        var controller = TestHelper.Profiles(db, participant.ParticipantId, Roles.Participant);

        var result = await controller.GetParticipant(participant.ParticipantId);

        Assert.Equal(200, TestHelper.StatusOf(result));
        Assert.Equal(participant.Email, TestHelper.ValueOf<ProfileResponse>(result).Email);
        Assert.Null(typeof(ProfileResponse).GetProperty("PasswordHash")); // the response has no password hash field
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsProfile_Returns403()
    {
        using var db = TestHelper.NewDb();
        var me = TestHelper.AddParticipant(db, "me@test.com");
        var other = TestHelper.AddParticipant(db, "other@test.com");
        var controller = TestHelper.Profiles(db, me.ParticipantId, Roles.Participant);

        var result = await controller.GetParticipant(other.ParticipantId);

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Participant_CanUpdateOwnProfile_ButNotAnothers()
    {
        using var db = TestHelper.NewDb();
        var me = TestHelper.AddParticipant(db, "me@test.com");
        var other = TestHelper.AddParticipant(db, "other@test.com");
        var controller = TestHelper.Profiles(db, me.ParticipantId, Roles.Participant);
        var update = new UpdateProfileRequest { FullName = "Updated Name", PhoneNumber = "0821234567" };

        var own = await controller.UpdateParticipant(me.ParticipantId, update);
        var notOwn = await controller.UpdateParticipant(other.ParticipantId, update);

        Assert.Equal(200, TestHelper.StatusOf(own));
        Assert.Equal("Updated Name", db.Participants.Single(p => p.ParticipantId == me.ParticipantId).FullName);
        Assert.Equal(403, TestHelper.StatusOf(notOwn));
    }

    [Fact]
    public async Task Organiser_CanViewAndUpdateOwnProfile_ButNotAnothers()
    {
        using var db = TestHelper.NewDb();
        var me = TestHelper.AddOrganiser(db, "me@test.com");
        var other = TestHelper.AddOrganiser(db, "other@test.com");
        var controller = TestHelper.Profiles(db, me.OrganiserId, Roles.Organiser);

        Assert.Equal(200, TestHelper.StatusOf(await controller.GetOrganiser(me.OrganiserId)));
        Assert.Equal(403, TestHelper.StatusOf(await controller.GetOrganiser(other.OrganiserId)));

        var update = await controller.UpdateOrganiser(me.OrganiserId, new UpdateProfileRequest { FullName = "Updated Organiser" });
        Assert.Equal(200, TestHelper.StatusOf(update));
    }

    [Fact]
    public async Task Profile_WhenNotLoggedIn_Returns401()
    {
        using var db = TestHelper.NewDb();
        var participant = TestHelper.AddParticipant(db);

        var result = await TestHelper.Profiles(db).GetParticipant(participant.ParticipantId);

        Assert.Equal(401, TestHelper.StatusOf(result));
    }
}