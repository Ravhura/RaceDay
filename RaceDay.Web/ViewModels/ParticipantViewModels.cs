using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models;

namespace RaceDay.Web.ViewModels;

public class HomeViewModel
{
    public List<EventItem> Events { get; set; } = new();
    public int TotalUpcoming { get; set; }
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Type) || DateFrom.HasValue || DateTo.HasValue;
}

public class EventPageViewModel
{
    public EventItem Event { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
    public List<EnrolmentItem> MyEnrolments { get; set; } = new();   // this Participant's enrolments in THIS event
    public bool IsUpcoming { get; set; }
}

public class MyEnrolmentRow
{
    public EnrolmentItem Enrolment { get; set; } = new();
    public EventItem? Event { get; set; }
}

public class MyResultRow
{
    public EnrolmentItem Enrolment { get; set; } = new();
    public EventItem? Event { get; set; }
    public ResultItem? Result { get; set; }   // null = the Organiser has not captured it yet
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name can be at most 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9 ]{7,20}$", ErrorMessage = "Enter 7 to 20 digits (spaces and a leading + are allowed).")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;
    public string MemberSince { get; set; } = string.Empty;
}