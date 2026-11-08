using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data;

/// <summary>
/// DEVELOPMENT ONLY. Creates sample data (2 Organisers, 2 Participants, 5 Events, categories, enrolments, results)
/// with real password hashes (ASP.NET Core Identity hasher) so you can log in during the demo.
/// Event dates are relative to today, so there are always upcoming events and past events with results.
/// Every seeded account uses the password in <see cref="DevPassword"/>. Never use this in production.
/// </summary>
public static class DbSeeder
{
    public const string DevPassword = "RaceDay123";

    public static async Task SeedAsync(RaceDayDbContext db)
    {
        if (await db.Organisers.AnyAsync())
        {
            return; // already seeded
        }

        var hash = PasswordHelper.Hash(DevPassword);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var sipho = new Organiser { FullName = "Sipho Mokoena", Email = "sipho.mokoena@raceday.co.za", PasswordHash = hash, PhoneNumber = "0821234567" };
        var lindiwe = new Organiser { FullName = "Lindiwe Dube", Email = "lindiwe.dube@raceday.co.za", PasswordHash = hash, PhoneNumber = "0837654321" };
        var sedzani = new Participant { FullName = "Sedzani Ravhura", Email = "sedzanir@gmail.com", PasswordHash = hash, PhoneNumber = "0731112222" };
        var amanda = new Participant { FullName = "Amanda van Wyk", Email = "amanda.vanwyk@gmail.com", PasswordHash = hash, PhoneNumber = "0723334444" };

        // ----- Past events (they will have results) -----
        var joburg = new Event
        {
            Organiser = sipho,
            EventName = "Johannesburg City Marathon",
            Description = "A road run through the heart of Johannesburg with 10km and half marathon options.",
            EventDate = today.AddDays(-14),
            Location = "Johannesburg",
            DistanceKm = 21.10m,
            EventType = EventType.Run,
            Status = "Completed"
        };
        var durban = new Event
        {
            Organiser = lindiwe,
            EventName = "Durban Coastal Cycle Tour",
            Description = "A scenic cycle tour along the Durban coastline with 40km and 80km routes.",
            EventDate = today.AddDays(-45),
            Location = "Durban",
            DistanceKm = 80.00m,
            EventType = EventType.Cycle,
            Status = "Completed"
        };

        // ----- Upcoming events -----
        var pretoria = new Event
        {
            Organiser = sipho,
            EventName = "Pretoria Park Run",
            Description = "A friendly 5km park run for all ages and fitness levels.",
            EventDate = today.AddDays(14),
            Location = "Pretoria",
            DistanceKm = 5.00m,
            EventType = EventType.Run,
            Status = EventStatuses.Upcoming
        };
        var soweto = new Event
        {
            Organiser = sipho,
            EventName = "Soweto Fun Ride",
            Description = "A relaxed community fun ride through Soweto for all fitness levels.",
            EventDate = today.AddDays(30),
            Location = "Soweto",
            DistanceKm = 15.00m,
            EventType = EventType.Cycle,
            Status = EventStatuses.Upcoming
        };
        var capeTown = new Event
        {
            Organiser = lindiwe,
            EventName = "Cape Town Peninsula Walk",
            Description = "A scenic charity walk along the Cape Peninsula with sea views the whole way.",
            EventDate = today.AddDays(45),
            Location = "Cape Town",
            DistanceKm = 10.00m,
            EventType = EventType.Walk,
            Status = EventStatuses.Upcoming
        };

        var joburg10 = new Category { Event = joburg, CategoryName = "10km Run", DistanceKm = 10.00m, EntryFee = 150.00m };
        var joburg21 = new Category { Event = joburg, CategoryName = "21km Half Marathon", DistanceKm = 21.10m, EntryFee = 250.00m };
        var durban40 = new Category { Event = durban, CategoryName = "40km Cycle", DistanceKm = 40.00m, EntryFee = 300.00m };
        var durban80 = new Category { Event = durban, CategoryName = "80km Cycle", DistanceKm = 80.00m, EntryFee = 450.00m };
        var pretoriaU20 = new Category { Event = pretoria, CategoryName = "Under 20", DistanceKm = 5.00m, EntryFee = 50.00m };
        var pretoriaSenior = new Category { Event = pretoria, CategoryName = "Senior", DistanceKm = 5.00m, EntryFee = 80.00m };
        var soweto5 = new Category { Event = soweto, CategoryName = "5km Fun Ride", DistanceKm = 5.00m, EntryFee = 100.00m };
        var soweto15 = new Category { Event = soweto, CategoryName = "15km Fun Ride", DistanceKm = 15.00m, EntryFee = 150.00m };
        var capeTown5 = new Category { Event = capeTown, CategoryName = "5km Stroll", DistanceKm = 5.00m, EntryFee = 60.00m };
        var capeTown10 = new Category { Event = capeTown, CategoryName = "10km Walk", DistanceKm = 10.00m, EntryFee = 90.00m };

        // Stage 1: save everything that has generated IDs, so enrolments can use real IDs.
        db.AddRange(sipho, lindiwe, sedzani, amanda, joburg, durban, pretoria, soweto, capeTown,
                    joburg10, joburg21, durban40, durban80, pretoriaU20, pretoriaSenior,
                    soweto5, soweto15, capeTown5, capeTown10);
        await db.SaveChangesAsync();

        // Stage 2: enrolments. One of them is Pending so the amber status shows in the demo.
        var e1 = NewEnrolment(sedzani, joburg10, EnrolmentStatuses.Confirmed);
        var e2 = NewEnrolment(sedzani, durban40, EnrolmentStatuses.Confirmed);
        var e3 = NewEnrolment(amanda, joburg21, EnrolmentStatuses.Confirmed);
        var e4 = NewEnrolment(amanda, soweto5, EnrolmentStatuses.Confirmed);
        var e5 = NewEnrolment(sedzani, capeTown5, EnrolmentStatuses.Pending);
        var e6 = NewEnrolment(amanda, pretoriaSenior, EnrolmentStatuses.Confirmed);
        db.Enrolments.AddRange(e1, e2, e3, e4, e5, e6);
        await db.SaveChangesAsync();

        // Stage 3: results for the Johannesburg marathon only. The Durban tour has none yet,
        // so the Organiser can capture them live in the demo.
        db.Results.AddRange(
            new Result { EnrolmentId = e1.EnrolmentId, FinishTime = new TimeSpan(0, 52, 30), Position = 2 },
            new Result { EnrolmentId = e3.EnrolmentId, FinishTime = new TimeSpan(1, 58, 10), Position = 1 });
        await db.SaveChangesAsync();
    }

    private static Enrolment NewEnrolment(Participant participant, Category category, string status) => new()
    {
        ParticipantId = participant.ParticipantId,
        EventId = category.EventId,
        CategoryId = category.CategoryId,
        Status = status
    };
}