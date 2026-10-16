using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;

namespace AcxiomCRM.ViewModels;

public class CustomerReportViewModel
{
    public List<Customer> Customers { get; set; } = new();
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class LeadReportViewModel
{
    public List<Lead> Leads { get; set; } = new();
    public string? Status { get; set; }
    public string? Source { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class OpportunityReportViewModel
{
    public List<Opportunity> Opportunities { get; set; } = new();
    public string? Stage { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class PipelineReportViewModel
{
    public List<PipelineStageSummary> StageSummaries { get; set; } = new();
    public List<OwnerPipelineSummary> OwnerSummaries { get; set; } = new();
    public decimal GrandTotal { get; set; }
    public decimal GrandWeightedTotal { get; set; }
}

public class FollowUpReportViewModel
{
    public List<FollowUp> FollowUps { get; set; } = new();
    public string? Status { get; set; }
    public string? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ConversionReportViewModel
{
    public ConversionRateSummary Summary { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class AuditReportViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public string? UserFilter { get; set; }
    public string? ModuleFilter { get; set; }
    public string? ActionFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
