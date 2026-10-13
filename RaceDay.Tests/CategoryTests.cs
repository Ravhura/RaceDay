using RaceDay.Api.Authentication;
using RaceDay.Api.DTOs;

namespace RaceDay.Tests;

/// <summary>Organisers manage categories for their own events only.</summary>
public class CategoryTests
{
    [Fact]
    public async Task Organiser_CanCreateACategory_ForOwnEvent()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = TestHelper.Categories(db, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.CreateCategory(ev.EventId, new CategoryRequest { CategoryName = "Under 20", EntryFee = 80 });

        Assert.Equal(201, TestHelper.StatusOf(result));
        var created = TestHelper.ValueOf<CategoryResponse>(result);
        Assert.Equal(ev.EventId, created.EventId);
        Assert.Equal("Under 20", created.CategoryName);
    }

    [Fact]
    public async Task Organiser_CannotCreateACategory_ForAnotherOrganisersEvent_Returns403()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddOrganiser(db, "owner@test.com");
        var other = TestHelper.AddOrganiser(db, "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.OrganiserId);
        var controller = TestHelper.Categories(db, other.OrganiserId, Roles.Organiser);

        var result = await controller.CreateCategory(ev.EventId, new CategoryRequest { CategoryName = "Sneaky", EntryFee = 0 });

        Assert.Equal(403, TestHelper.StatusOf(result));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Participant_CannotCreateACategory_Returns403()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var participant = TestHelper.AddParticipant(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var controller = TestHelper.Categories(db, participant.ParticipantId, Roles.Participant);

        var result = await controller.CreateCategory(ev.EventId, new CategoryRequest { CategoryName = "Nope", EntryFee = 0 });

        Assert.Equal(403, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task CreateCategory_ForAnEventThatDoesNotExist_Returns404()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var controller = TestHelper.Categories(db, organiser.OrganiserId, Roles.Organiser);

        var result = await controller.CreateCategory(999, new CategoryRequest { CategoryName = "Ghost", EntryFee = 0 });

        Assert.Equal(404, TestHelper.StatusOf(result));
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnCategory()
    {
        using var db = TestHelper.NewDb();
        var organiser = TestHelper.AddOrganiser(db);
        var ev = TestHelper.AddEvent(db, organiser.OrganiserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Categories(db, organiser.OrganiserId, Roles.Organiser);

        var update = await controller.UpdateCategory(category.CategoryId, new CategoryRequest { CategoryName = "Senior", EntryFee = 200 });
        Assert.Equal(200, TestHelper.StatusOf(update));
        Assert.Equal("Senior", db.Categories.Single().CategoryName);

        var delete = await controller.DeleteCategory(category.CategoryId);
        Assert.Equal(204, TestHelper.StatusOf(delete));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Organiser_CannotUpdateOrDeleteAnotherOrganisersCategory_Returns403()
    {
        using var db = TestHelper.NewDb();
        var owner = TestHelper.AddOrganiser(db, "owner@test.com");
        var other = TestHelper.AddOrganiser(db, "other@test.com");
        var ev = TestHelper.AddEvent(db, owner.OrganiserId);
        var category = TestHelper.AddCategory(db, ev.EventId);
        var controller = TestHelper.Categories(db, other.OrganiserId, Roles.Organiser);

        var update = await controller.UpdateCategory(category.CategoryId, new CategoryRequest { CategoryName = "Hijacked", EntryFee = 0 });
        var delete = await controller.DeleteCategory(category.CategoryId);

        Assert.Equal(403, TestHelper.StatusOf(update));
        Assert.Equal(403, TestHelper.StatusOf(delete));
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task Organiser_CannotDeleteACategoryThatHasEnrolments_Returns409()
    {
        using var db = TestHelper.NewDb();
        var s = TestHelper.NewScenario(db);
        var controller = TestHelper.Categories(db, s.Organiser.OrganiserId, Roles.Organiser);

        var result = await controller.DeleteCategory(s.Category.CategoryId);

        Assert.Equal(409, TestHelper.StatusOf(result));
        Assert.Single(db.Categories);
    }

    [Fact]
    public async Task ListCategories_ForAnEventThatDoesNotExist_Returns404()
    {
        using var db = TestHelper.NewDb();
        var participant = TestHelper.AddParticipant(db);

        var result = await TestHelper.Categories(db, participant.ParticipantId, Roles.Participant).GetEventCategories(999);

        Assert.Equal(404, TestHelper.StatusOf(result));
    }
}