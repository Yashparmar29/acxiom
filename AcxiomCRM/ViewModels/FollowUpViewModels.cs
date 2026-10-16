using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Validation;

namespace AcxiomCRM.ViewModels;

public class FollowUpListViewModel
{
    public List<FollowUp> FollowUps { get; set; } = new();
    public string? SearchTerm { get; set; }
    public FollowUpStatus? Status { get; set; }
    public FollowUpType? Type { get; set; }
    public string? FilterType { get; set; } = "all";
    public string? AssignedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public SelectList? UsersList { get; set; }
}

public class FollowUpCreateViewModel
{
    [Display(Name = "Related Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Related Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up date is required.")]
    [DataType(DataType.Date)]
    [FutureOrToday(ErrorMessage = "Follow-up date cannot be earlier than today.")]
    [Display(Name = "Follow-Up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Type")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    public string? Remarks { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? OpportunitiesList { get; set; }
    public SelectList? UsersList { get; set; }
}

public class FollowUpCompleteViewModel
{
    public int FollowUpId { get; set; }
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    public string? Remarks { get; set; }
}

public class FollowUpRescheduleViewModel
{
    public int FollowUpId { get; set; }
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "New Follow-up date is required.")]
    [DataType(DataType.Date)]
    [FutureOrToday(ErrorMessage = "Follow-up date cannot be earlier than today.")]
    [Display(Name = "New Follow-Up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;

    [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    public string? Remarks { get; set; }
}
