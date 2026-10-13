using System.ComponentModel.DataAnnotations;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

/// <summary>The validation rules on the request classes. In the real API these produce an automatic 400 response.</summary>
public class ValidationTests
{
    private static bool IsValid(object model)
    {
        var errors = new List<ValidationResult>();
        return Validator.TryValidateObject(model, new ValidationContext(model), errors, validateAllProperties: true);
    }

    // ---------- Registration ----------

    [Fact]
    public void RegisterRequest_WithValidDetails_IsValid()
    {
        var request = new RegisterRequest { FullName = "Jane Runner", Email = "jane@test.com", Password = "Passw0rd1" };
        Assert.True(IsValid(request));
    }

    [Theory]
    [InlineData("short1")]        // too short
    [InlineData("onlyletters")]   // no digit
    [InlineData("12345678")]      // no letter
    public void RegisterRequest_WithAWeakPassword_IsInvalid(string password)
    {
        var request = new RegisterRequest { FullName = "Jane Runner", Email = "jane@test.com", Password = password };
        Assert.False(IsValid(request));
    }

    [Fact]
    public void RegisterRequest_WithAnInvalidEmail_IsInvalid()
    {
        var request = new RegisterRequest { FullName = "Jane Runner", Email = "not-an-email", Password = "Passw0rd1" };
        Assert.False(IsValid(request));
    }

    [Fact]
    public void RegisterRequest_WithoutAName_IsInvalid()
    {
        var request = new RegisterRequest { FullName = "", Email = "jane@test.com", Password = "Passw0rd1" };
        Assert.False(IsValid(request));
    }

    // ---------- Events ----------

    [Fact]
    public void EventRequest_WithAllFields_IsValid()
    {
        Assert.True(IsValid(TestHelper.ValidEventRequest()));
    }

    [Fact]
    public void EventRequest_WithoutAnEventType_IsInvalid()
    {
        var request = TestHelper.ValidEventRequest();
        request.EventType = null;
        Assert.False(IsValid(request));
    }

    [Fact]
    public void EventRequest_WithoutADate_IsInvalid()
    {
        var request = TestHelper.ValidEventRequest();
        request.EventDate = null;
        Assert.False(IsValid(request));
    }

    [Fact]
    public void EventRequest_WithZeroDistance_IsInvalid()
    {
        var request = TestHelper.ValidEventRequest();
        request.DistanceKm = 0;
        Assert.False(IsValid(request));
    }

    [Fact]
    public void EventRequest_WithoutALocation_IsInvalid()
    {
        var request = TestHelper.ValidEventRequest();
        request.Location = "";
        Assert.False(IsValid(request));
    }

    // ---------- Categories, enrolments, results, profiles ----------

    [Fact]
    public void CategoryRequest_WithANegativeFee_IsInvalid()
    {
        Assert.False(IsValid(new CategoryRequest { CategoryName = "Bad Fee", EntryFee = -5 }));
    }

    [Fact]
    public void CreateEnrolmentRequest_WithZeroIds_IsInvalid()
    {
        Assert.False(IsValid(new CreateEnrolmentRequest { EventId = 0, CategoryId = 0 }));
    }

    [Fact]
    public void UpdateResultRequest_WithPositionZero_IsInvalid()
    {
        Assert.False(IsValid(new UpdateResultRequest { FinishTime = new TimeSpan(0, 30, 0), Position = 0 }));
    }

    [Fact]
    public void UpdateResultRequest_WithoutAFinishTime_IsInvalid()
    {
        Assert.False(IsValid(new UpdateResultRequest { FinishTime = null, Position = 1 }));
    }

    [Fact]
    public void UpdateProfileRequest_WithAnInvalidPhoneNumber_IsInvalid()
    {
        Assert.False(IsValid(new UpdateProfileRequest { FullName = "Name", PhoneNumber = "abc" }));
    }

    [Fact]
    public void UpdateProfileRequest_WithoutAPhoneNumber_IsValid()
    {
        Assert.True(IsValid(new UpdateProfileRequest { FullName = "Name", PhoneNumber = null }));
    }
}