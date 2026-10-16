using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Validation;

namespace AcxiomCRM.ViewModels;

public class LeadListViewModel
{
    public List<Lead> Leads { get; set; } = new();
    public string? SearchTerm { get; set; }
    public LeadStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public SelectList? UsersList { get; set; }
}

public class LeadCreateEditViewModel
{
    public int LeadId { get; set; }

    public string? LeadCode { get; set; }

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string? Email { get; set; }

    [IndianPhone(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    public string? Phone { get; set; }

    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 100000000, ErrorMessage = "Expected Value must be between 0 and 100,000,000.")]
    [Display(Name = "Expected Value (₹)")]
    public decimal ExpectedValue { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public SelectList? UsersList { get; set; }
}

public class LeadChangeStatusViewModel
{
    public int LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public LeadStatus CurrentStatus { get; set; }

    [Required(ErrorMessage = "Select a new status.")]
    [Display(Name = "New Status")]
    public LeadStatus NewStatus { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    public IReadOnlyList<LeadStatus> AllowedStatuses { get; set; } = Array.Empty<LeadStatus>();
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Create Opportunity from Lead")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    public string? OpportunityName { get; set; }
}
