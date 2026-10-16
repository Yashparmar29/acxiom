using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Validation;

namespace AcxiomCRM.ViewModels;

public class OpportunityListViewModel
{
    public List<Opportunity> Opportunities { get; set; } = new();
    public string? SearchTerm { get; set; }
    public OpportunityStage? Stage { get; set; }
    public OpportunityStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public SelectList? UsersList { get; set; }
}

public class OpportunityCreateEditViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [AmountGreaterThanZero(ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Amount (₹)")]
    public decimal Amount { get; set; }

    [Display(Name = "Stage")]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required(ErrorMessage = "Probability is required.")]
    [ValidProbability(ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 10;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [CloseDateNotInPast(ErrorMessage = "Expected Close Date cannot be in the past.")]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    [Display(Name = "Status")]
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
    public string? Source { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? UsersList { get; set; }
}

public class OpportunityPipelineViewModel
{
    public Dictionary<OpportunityStage, List<Opportunity>> Board { get; set; } = new();
    public string? AssignedTo { get; set; }
    public SelectList? UsersList { get; set; }
}

public class OpportunityChangeStageViewModel
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public OpportunityStage CurrentStage { get; set; }

    [Required(ErrorMessage = "Select a new stage.")]
    [Display(Name = "New Stage")]
    public OpportunityStage NewStage { get; set; }
}
