using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.ViewModels;



public class ActivityCreateEditViewModel
{
    public int ActivityId { get; set; }

    [Display(Name = "Activity Type")]
    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Related Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Display(Name = "Status")]
    public ActivityStatus Status { get; set; } = ActivityStatus.Completed;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public SelectList? CustomersList { get; set; }
    public SelectList? LeadsList { get; set; }
    public SelectList? UsersList { get; set; }
}
