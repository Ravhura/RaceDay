using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Authentication;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>A pretend Azure Blob Storage, so the banner tests need no internet and no Azure account.</summary>
public class FakeBlobStorage : IBlobStorageService
{
    public bool ShouldFail { get; set; }
    public List<string> Deleted { get; } = new();

    public Task<string> UploadAsync(Stream content, string contentType, string blobName)
    {
        if (ShouldFail) throw new InvalidOperationException("Storage is down.");
        return Task.FromResult("https://fake.blob.core.windows.net/event-banners/" + blobName);
    }

    public Task DeleteAsync(string blobUrl)
    {
        Deleted.Add(blobUrl);
        return Task.CompletedTask;
    }
}

/// <summary>The banner upload: only the event's Organiser, only real images, and the address is saved on the event.</summary>
public class BannerTests
{
    // The first 12 bytes of a real PNG file.
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };

    private static IFormFile MakeFile(byte[] bytes, string contentType = "image/png", long? length = null)
    {
        return new FormFile(new MemoryStream(bytes), 0, length ?? bytes.Length, "file", "banner.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static BannersController Controller(RaceDayDbContext db, FakeBlobStorage storage, int? userId, string? role)
    {
        var controller = new BannersController(new BannerService(db, storage));
        var httpContext = new DefaultHttpContext { Session = new FakeSession() };
        if (userId.HasValue && role != null)
        {
            httpContext.Session.SetInt32(SessionKeys.UserId, userId.Value);
            httpContext.Session.SetString(SessionKeys.Role, role);
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task Organiser_CanUploadABanner_ForTheirOwnEvent_AndTheAddressIsSaved()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = Controller(db, new FakeBlobStorage(), organiser.OrganiserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile(PngHeader));

        Assert.Equal(200, TestHelper.StatusOf(result));
        var url = TestHelper.ValueOf<EventResponse>(result).BannerUrl;
        Assert.StartsWith("https://fake.blob.core.windows.net/event-banners/", url);
        Assert.Equal(url, db.Events.Single().BannerUrl);   // saved in the database
    }

    [Fact]
    public async Task Organiser_CannotUploadABanner_ForAnotherOrganisersEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddOrganiser(db, "owner@test.com");
        var other = TestHelper.AddOrganiser(db, "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.OrganiserId);
        var controller = Controller(db, new FakeBlobStorage(), other.OrganiserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile(PngHeader));

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Null(db.Events.Single().BannerUrl);
    }

    [Fact]
    public async Task Participant_CannotUploadABanner_Returns403()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var participant = TestHelper.AddParticipant(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = Controller(db, new FakeBlobStorage(), participant.ParticipantId, Roles.Participant);

        var result = await controller.UploadBanner(ev.EventId, MakeFile(PngHeader));

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task NotLoggedIn_CannotUploadABanner_Returns401()
    {
        using var db = TestHelper.NewDb();
        var controller = Controller(db, new FakeBlobStorage(), null, null);

        var result = await controller.UploadBanner(1, MakeFile(PngHeader));

        Assert.Equal(401, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Upload_ForAnEventThatDoesNotExist_Returns404()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var controller = Controller(db, new FakeBlobStorage(), organiser.OrganiserId, Roles.Organiser);

        var result = await controller.UploadBanner(999, MakeFile(PngHeader));

        Assert.Equal(404, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Upload_OfAFileThatIsNotAnImage_Returns400()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = Controller(db, new FakeBlobStorage(), organiser.OrganiserId, Roles.Organiser);

        // A text file pretending to be a PNG.
        var fake = MakeFile(System.Text.Encoding.UTF8.GetBytes("this is not an image file"), "image/png");
        var wrongType = MakeFile(PngHeader, "text/plain");

        Assert.Equal(400, TestHelper.StatusOf(await controller.UploadBanner(ev.EventId, fake)));
        Assert.Equal(400, TestHelper.StatusOf(await controller.UploadBanner(ev.EventId, wrongType)));
        Assert.Null(db.Events.Single().BannerUrl);
    }

    [Fact]
    public async Task Upload_OfAFileOver5MB_Returns400()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = Controller(db, new FakeBlobStorage(), organiser.OrganiserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile(PngHeader, "image/png", length: 6 * 1024 * 1024));

        Assert.Equal(400, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Upload_WhenCloudStorageIsDown_Returns503_AndSavesNothing()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = Controller(db, new FakeBlobStorage { ShouldFail = true }, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.UploadBanner(ev.EventId, MakeFile(PngHeader));

        Assert.Equal(503, TestHelper.StatusOf(result));
        Assert.Null(db.Events.Single().BannerUrl);
    }

    [Fact]
    public async Task ReplacingABanner_RemovesTheOldOne()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var storage = new FakeBlobStorage();
        var controller = Controller(db, storage, organiser.OrganiserId, Roles.Organiser);

        var first = await controller.UploadBanner(ev.EventId, MakeFile(PngHeader));
        var firstUrl = TestHelper.ValueOf<EventResponse>(first).BannerUrl;
        await controller.UploadBanner(ev.EventId, MakeFile(PngHeader));

        Assert.Equal(firstUrl, Assert.Single(storage.Deleted));
    }
}