using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// DEVELOPMENT ONLY. Re-creates the Part 1 sample data (2 Organisers, 2 Participants, 3 Events,
/// categories, enrolments, results) with real password hashes so you can log in during the demo.
/// Every seeded account uses the password in <see cref="DevPassword"/>. Never use this in production.
/// </summary>
public static class DbSeeder
{
    public const string DevPassword = "RaceDay123";

    public static async Task SeedAsync(RaceDayDbContext db, PasswordService passwords)
    {
        if (await db.Organisers.AnyAsync())
        {
            return; // already seeded
        }

        var hash = passwords.Hash(DevPassword);

        var sipho = new Organiser { FullName = "Sipho Mokoena", Email = "sipho.mokoena@raceday.co.za", PasswordHash = hash, PhoneNumber = "0821234567" };
        var lindiwe = new Organiser { FullName = "Lindiwe Dube", Email = "lindiwe.dube@raceday.co.za", PasswordHash = hash, PhoneNumber = "0837654321" };
        var sedzani = new Participant { FullName = "Sedzani Ravhura", Email = "sedzanir@gmail.com", PasswordHash = hash, PhoneNumber = "0731112222" };
        var amanda = new Participant { FullName = "Amanda van Wyk", Email = "amanda.vanwyk@gmail.com", PasswordHash = hash, PhoneNumber = "0723334444" };

        var joburg = new Event
        {
            Organiser = sipho,
            EventName = "Johannesburg City Marathon",
            Description = "A road run through the heart of Johannesburg with 10km and half marathon options.",
            EventDate = new DateOnly(2026, 10, 10),
            Location = "Johannesburg",
            DistanceKm = 21.10m,
            EventType = EventType.Run,
            Status = EventStatuses.Upcoming
        };
        var soweto = new Event
        {
            Organiser = sipho,
            EventName = "Soweto Fun Ride",
            Description = "A relaxed community fun ride through Soweto for all fitness levels.",
            EventDate = new DateOnly(2026, 11, 2),
            Location = "Soweto",
            DistanceKm = 15.00m,
            EventType = EventType.Cycle,
            Status = EventStatuses.Upcoming
        };
        var durban = new Event
        {
            Organiser = lindiwe,
            EventName = "Durban Coastal Cycle Tour",
            Description = "A scenic cycle tour along the Durban coastline with 40km and 80km routes.",
            EventDate = new DateOnly(2026, 9, 20),
            Location = "Durban",
            DistanceKm = 80.00m,
            EventType = EventType.Cycle,
            Status = EventStatuses.Upcoming
        };

        var joburg10 = new Category { Event = joburg, CategoryName = "10km Run", DistanceKm = 10.00m, EntryFee = 150.00m };
        var joburg21 = new Category { Event = joburg, CategoryName = "21km Half Marathon", DistanceKm = 21.10m, EntryFee = 250.00m };
        var soweto5 = new Category { Event = soweto, CategoryName = "5km Fun Ride", DistanceKm = 5.00m, EntryFee = 100.00m };
        var soweto15 = new Category { Event = soweto, CategoryName = "15km Fun Ride", DistanceKm = 15.00m, EntryFee = 150.00m };
        var durban40 = new Category { Event = durban, CategoryName = "40km Cycle", DistanceKm = 40.00m, EntryFee = 300.00m };
        var durban80 = new Category { Event = durban, CategoryName = "80km Cycle", DistanceKm = 80.00m, EntryFee = 450.00m };

        // Stage 1: save everything that has generated IDs, so enrolments can use real IDs.
        db.AddRange(sipho, lindiwe, sedzani, amanda, joburg, soweto, durban,
                    joburg10, joburg21, soweto5, soweto15, durban40, durban80);
        await db.SaveChangesAsync();

        // Stage 2: enrolments (same four as Part 1).
        var e1 = NewEnrolment(sedzani, joburg10);
        var e2 = NewEnrolment(sedzani, durban40);
        var e3 = NewEnrolment(amanda, joburg21);
        var e4 = NewEnrolment(amanda, soweto5);
        db.Enrolments.AddRange(e1, e2, e3, e4);
        await db.SaveChangesAsync();

        // Stage 3: results for two of the enrolments (same as Part 1).
        db.Results.AddRange(
            new Result { EnrolmentId = e1.EnrolmentId, FinishTime = new TimeSpan(0, 52, 30), Position = 5 },
            new Result { EnrolmentId = e3.EnrolmentId, FinishTime = new TimeSpan(1, 58, 10), Position = 2 });
        await db.SaveChangesAsync();
    }

    private static Enrolment NewEnrolment(Participant participant, Category category) => new()
    {
        ParticipantId = participant.ParticipantId,
        EventId = category.EventId,
        CategoryId = category.CategoryId,
        Status = EnrolmentStatuses.Confirmed
    };
}