using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>Shared helpers so each test stays short and easy to read.</summary>
public static class TestHelper
{
    public const string Password = "Passw0rd1";

    // ---------- A fresh, empty, in-memory database for every test ----------
    public static RaceDayDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RaceDayDbContext(options);
    }

    // ---------- Controllers (pass a userId and role to act as a logged-in user; pass nothing for "not logged in") ----------
    public static AuthController Auth(RaceDayDbContext db, int? userId = null, string? role = null) =>
        WithSession(new AuthController(new AuthService(db)), userId, role);

    public static ProfilesController Profiles(RaceDayDbContext db, int? userId = null, string? role = null) =>
        WithSession(new ProfilesController(new ProfileService(db)), userId, role);

    public static EventsController Events(RaceDayDbContext db, int? userId = null, string? role = null) =>
        WithSession(new EventsController(new EventService(db)), userId, role);

    public static CategoriesController Categories(RaceDayDbContext db, int? userId = null, string? role = null) =>
        WithSession(new CategoriesController(new CategoryService(db)), userId, role);

    public static EnrolmentsController Enrolments(RaceDayDbContext db, int? userId = null, string? role = null) =>
        WithSession(new EnrolmentsController(new EnrolmentService(db)), userId, role);

    public static ResultsController Results(RaceDayDbContext db, int? userId = null, string? role = null) =>
        WithSession(new ResultsController(new ResultService(db)), userId, role);

    // Gives the controller a fake HTTP request with a fake session. If a user is given, they are "logged in".
    private static T WithSession<T>(T controller, int? userId, string? role) where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext { Session = new FakeSession() };
        if (userId.HasValue && role != null)
        {
            httpContext.Session.SetInt32(SessionKeys.UserId, userId.Value);
            httpContext.Session.SetString(SessionKeys.Role, role);
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    // ---------- Sample data saved straight into the test database ----------
    public static Organiser AddOrganiser(RaceDayDbContext db, string email = "organiser@test.com")
    {
        var organiser = new Organiser { FullName = "Test Organiser", Email = email, PasswordHash = PasswordHelper.Hash(Password) };
        db.Organisers.Add(organiser);
        db.SaveChanges();
        return organiser;
    }

    public static Participant AddParticipant(RaceDayDbContext db, string email = "participant@test.com")
    {
        var participant = new Participant { FullName = "Test Participant", Email = email, PasswordHash = PasswordHelper.Hash(Password) };
        db.Participants.Add(participant);
        db.SaveChanges();
        return participant;
    }

    public static Event AddEvent(RaceDayDbContext db, int organiserId, string name = "Test Run")
    {
        var ev = new Event
        {
            OrganiserId = organiserId,
            EventName = name,
            Description = "A test event.",
            EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            Location = "Johannesburg",
            DistanceKm = 10,
            EventType = EventType.Run
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(RaceDayDbContext db, int eventId, string name = "10km")
    {
        var category = new Category { EventId = eventId, CategoryName = name, DistanceKm = 10, EntryFee = 150 };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(RaceDayDbContext db, int participantId, int categoryId)
    {
        var category = db.Categories.First(c => c.CategoryId == categoryId);
        var enrolment = new Enrolment { ParticipantId = participantId, EventId = category.EventId, CategoryId = categoryId };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }

    public static Result AddResult(RaceDayDbContext db, int enrolmentId)
    {
        var result = new Result { EnrolmentId = enrolmentId, FinishTime = new TimeSpan(0, 52, 30), Position = 5 };
        db.Results.Add(result);
        db.SaveChanges();
        return result;
    }

    /// <summary>One Organiser with an event and category, and one Participant enrolled in it.</summary>
    public static (Organiser Organiser, Participant Participant, Event Event, Category Category, Enrolment Enrolment)
        NewScenario(RaceDayDbContext db)
    {
        var organiser = AddOrganiser(db);
        var participant = AddParticipant(db);
        var ev = AddEvent(db, organiser.OrganiserId);
        var category = AddCategory(db, ev.EventId);
        var enrolment = AddEnrolment(db, participant.ParticipantId, category.CategoryId);
        return (organiser, participant, ev, category, enrolment);
    }

    // ---------- Valid request bodies ----------
    public static EventRequest ValidEventRequest(string name = "New Event") => new EventRequest
    {
        EventName = name,
        Description = "A test event.",
        EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
        Location = "Pretoria",
        DistanceKm = 5,
        EventType = EventType.Run
    };

    // ---------- Reading what a controller returned ----------
    /// <summary>The HTTP status code of a controller result (200, 201, 401, 403...).</summary>
    public static int StatusOf(ActionResult result)
    {
        if (result is IStatusCodeActionResult withCode && withCode.StatusCode.HasValue)
        {
            return withCode.StatusCode.Value;
        }
        throw new InvalidOperationException("The result has no status code.");
    }

    /// <summary>The data returned in the response body.</summary>
    public static T ValueOf<T>(ActionResult result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        return Assert.IsType<T>(objectResult.Value);
    }
}