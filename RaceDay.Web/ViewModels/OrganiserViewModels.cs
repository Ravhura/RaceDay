using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models;

namespace RaceDay.Web.ViewModels;

public class DashboardEventRow
{
    public EventItem Event { get; set; } = new();
    public int Enrolments { get; set; }
}

public class DashboardViewModel
{
    public List<DashboardEventRow> Events { get; set; } = new();
    public int TotalEnrolments { get; set; }
}

public class EventDetailsViewModel
{
    public EventItem Event { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
    public int EnrolmentCount { get; set; }
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150, ErrorMessage = "The name can be at most 150 characters.")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the event.")]
    [StringLength(1000, ErrorMessage = "The description can be at most 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event date.")]
    [Display(Name = "Event date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150, ErrorMessage = "The location can be at most 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Event banner image")]
    public IFormFile? BannerFile { get; set; }

    /// <summary>The banner the event already has (shown on the edit page).</summary>
    public string? BannerUrl { get; set; }
}

public class ManageCategoriesViewModel
{
    public EventItem Event { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the category name.")]
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km), optional")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee (use 0 for free).")]
    [Range(0, 999999.99, ErrorMessage = "The entry fee must be 0 or more.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; } = 0;
}

public class ManageEnrolmentsViewModel
{
    public EventItem Event { get; set; } = new();
    public List<EnrolmentItem> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentItem Enrolment { get; set; } = new();
    public ResultItem? Result { get; set; }
}

public class ManageResultsViewModel
{
    public EventItem Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int ResultId { get; set; }   // 0 = a new result, otherwise we are editing one
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the finish time.")]
    [RegularExpression(@"^([0-1]?\d|2[0-3]):[0-5]\d:[0-5]\d$",
        ErrorMessage = "Enter the time as hh:mm:ss, for example 00:52:30.")]
    [Display(Name = "Finish time (hh:mm:ss)")]
    public string FinishTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the finishing position.")]
    [Range(1, 100000, ErrorMessage = "The position must be 1 or higher.")]
    [Display(Name = "Finishing position")]
    public int? Position { get; set; }
}