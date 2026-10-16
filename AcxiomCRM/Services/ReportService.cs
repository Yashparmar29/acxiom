using System.Text;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public class PipelineStageSummary
{
    public OpportunityStage Stage { get; set; }
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class OwnerPipelineSummary
{
    public string OwnerName { get; set; } = string.Empty;
    public int OpportunityCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class ConversionRateSummary
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public decimal ConversionPercentage => TotalLeads > 0 ? Math.Round((decimal)ConvertedLeads / TotalLeads * 100m, 2) : 0;
    public decimal TotalConvertedRevenue { get; set; }
}

public interface IReportService
{
    Task<(List<Customer> Customers, int TotalCount)> GetCustomerReportAsync(string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15);
    Task<(List<Lead> Leads, int TotalCount)> GetLeadReportAsync(string? status, string? source, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15);
    Task<(List<Opportunity> Opportunities, int TotalCount)> GetOpportunityReportAsync(string? stage, string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15);
    Task<(List<PipelineStageSummary> StageSummaries, List<OwnerPipelineSummary> OwnerSummaries, decimal GrandTotal, decimal GrandWeightedTotal)> GetPipelineReportAsync();
    Task<ConversionRateSummary> GetConversionReportAsync(DateTime? fromDate, DateTime? toDate);
    Task<(List<FollowUp> FollowUps, int TotalCount)> GetFollowUpReportAsync(string? status, string? type, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15);

    Task<byte[]> ExportCustomerReportCsvAsync(string? status, DateTime? fromDate, DateTime? toDate);
    Task<byte[]> ExportLeadReportCsvAsync(string? status, string? source, DateTime? fromDate, DateTime? toDate);
    Task<byte[]> ExportOpportunityReportCsvAsync(string? stage, string? status, DateTime? fromDate, DateTime? toDate);
    Task<byte[]> ExportPipelineReportCsvAsync();
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IOwnershipScopeHelper _scopeHelper;

    public ReportService(ApplicationDbContext context, IOwnershipScopeHelper scopeHelper)
    {
        _context = context;
        _scopeHelper = scopeHelper;
    }

    public async Task<(List<Customer> Customers, int TotalCount)> GetCustomerReportAsync(string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15)
    {
        var query = _scopeHelper.ApplyCustomerScope(_context.Customers.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, true, out var cs))
        {
            query = query.Where(c => c.Status == cs);
        }

        if (fromDate.HasValue) query = query.Where(c => c.CreatedDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(c => c.CreatedDate < toDate.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var items = await query.Include(c => c.AssignedUser)
                               .OrderByDescending(c => c.CreatedDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Lead> Leads, int TotalCount)> GetLeadReportAsync(string? status, string? source, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15)
    {
        var query = _scopeHelper.ApplyLeadScope(_context.Leads.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeadStatus>(status, true, out var ls))
        {
            query = query.Where(l => l.Status == ls);
        }

        if (!string.IsNullOrWhiteSpace(source) && Enum.TryParse<LeadSource>(source, true, out var lsrc))
        {
            query = query.Where(l => l.Source == lsrc);
        }

        if (fromDate.HasValue) query = query.Where(l => l.CreatedDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(l => l.CreatedDate < toDate.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var items = await query.Include(l => l.AssignedUser)
                               .Include(l => l.ConvertedCustomer)
                               .OrderByDescending(l => l.CreatedDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Opportunity> Opportunities, int TotalCount)> GetOpportunityReportAsync(string? stage, string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15)
    {
        var query = _scopeHelper.ApplyOpportunityScope(_context.Opportunities.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(stage) && Enum.TryParse<OpportunityStage>(stage, true, out var ost))
        {
            query = query.Where(o => o.Stage == ost);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OpportunityStatus>(status, true, out var osta))
        {
            query = query.Where(o => o.Status == osta);
        }

        if (fromDate.HasValue) query = query.Where(o => o.CreatedDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(o => o.CreatedDate < toDate.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var items = await query.Include(o => o.Customer)
                               .Include(o => o.AssignedUser)
                               .OrderByDescending(o => o.CreatedDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, total);
    }

    public async Task<(List<PipelineStageSummary> StageSummaries, List<OwnerPipelineSummary> OwnerSummaries, decimal GrandTotal, decimal GrandWeightedTotal)> GetPipelineReportAsync()
    {
        var opps = await _scopeHelper.ApplyOpportunityScope(_context.Opportunities.AsNoTracking())
                                     .Include(o => o.AssignedUser)
                                     .ToListAsync();

        var stageSummaries = Enum.GetValues<OpportunityStage>().Select(st =>
        {
            var matching = opps.Where(o => o.Stage == st).ToList();
            return new PipelineStageSummary
            {
                Stage = st,
                Count = matching.Count,
                TotalAmount = matching.Sum(m => m.Amount),
                WeightedAmount = matching.Sum(m => m.WeightedAmount)
            };
        }).ToList();

        var ownerSummaries = opps.GroupBy(o => o.AssignedUser?.FullName ?? o.AssignedUser?.UserName ?? "Unassigned")
            .Select(g => new OwnerPipelineSummary
            {
                OwnerName = g.Key,
                OpportunityCount = g.Count(),
                TotalAmount = g.Sum(o => o.Amount),
                WeightedAmount = g.Sum(o => o.WeightedAmount)
            }).ToList();

        var grandTotal = opps.Sum(o => o.Amount);
        var grandWeighted = opps.Sum(o => o.WeightedAmount);

        return (stageSummaries, ownerSummaries, grandTotal, grandWeighted);
    }

    public async Task<ConversionRateSummary> GetConversionReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _scopeHelper.ApplyLeadScope(_context.Leads.AsNoTracking());

        if (fromDate.HasValue) query = query.Where(l => l.CreatedDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(l => l.CreatedDate < toDate.Value.Date.AddDays(1));

        var leads = await query.ToListAsync();
        var converted = leads.Where(l => l.Status == LeadStatus.Converted).ToList();

        var convertedCustomerIds = converted.Where(l => l.ConvertedCustomerId.HasValue).Select(l => l.ConvertedCustomerId!.Value).ToList();
        var wonRevenue = await _context.Opportunities
            .Where(o => o.Status == OpportunityStatus.Won && convertedCustomerIds.Contains(o.CustomerId))
            .SumAsync(o => o.Amount);

        return new ConversionRateSummary
        {
            TotalLeads = leads.Count,
            ConvertedLeads = converted.Count,
            TotalConvertedRevenue = wonRevenue
        };
    }

    public async Task<(List<FollowUp> FollowUps, int TotalCount)> GetFollowUpReportAsync(string? status, string? type, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15)
    {
        var query = _scopeHelper.ApplyFollowUpScope(_context.FollowUps.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FollowUpStatus>(status, true, out var fst))
        {
            query = query.Where(f => f.Status == fst);
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<FollowUpType>(type, true, out var ftp))
        {
            query = query.Where(f => f.FollowUpType == ftp);
        }

        if (fromDate.HasValue) query = query.Where(f => f.FollowUpDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(f => f.FollowUpDate < toDate.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var items = await query.Include(f => f.Customer)
                               .Include(f => f.Lead)
                               .Include(f => f.AssignedUser)
                               .OrderByDescending(f => f.FollowUpDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, total);
    }

    public async Task<byte[]> ExportCustomerReportCsvAsync(string? status, DateTime? fromDate, DateTime? toDate)
    {
        var (customers, _) = await GetCustomerReportAsync(status, fromDate, toDate, 1, 10000);
        var sb = new StringBuilder();
        sb.WriteLine("CustomerCode,CustomerName,Email,Phone,CompanyName,Status,City,State,AssignedTo,CreatedDate");
        foreach (var c in customers)
        {
            sb.WriteLine($"\"{c.CustomerCode}\",\"{c.CustomerName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.CompanyName}\",\"{c.Status}\",\"{c.City}\",\"{c.State}\",\"{c.AssignedUser?.FullName}\",\"{c.CreatedDate:yyyy-MM-dd}\"");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportLeadReportCsvAsync(string? status, string? source, DateTime? fromDate, DateTime? toDate)
    {
        var (leads, _) = await GetLeadReportAsync(status, source, fromDate, toDate, 1, 10000);
        var sb = new StringBuilder();
        sb.WriteLine("LeadCode,LeadName,Email,Phone,CompanyName,Source,Status,Priority,ExpectedValue,AssignedTo,CreatedDate");
        foreach (var l in leads)
        {
            sb.WriteLine($"\"{l.LeadCode}\",\"{l.LeadName}\",\"{l.Email}\",\"{l.Phone}\",\"{l.CompanyName}\",\"{l.Source}\",\"{l.Status}\",\"{l.Priority}\",{l.ExpectedValue},\"{l.AssignedUser?.FullName}\",\"{l.CreatedDate:yyyy-MM-dd}\"");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportOpportunityReportCsvAsync(string? stage, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var (opps, _) = await GetOpportunityReportAsync(stage, status, fromDate, toDate, 1, 10000);
        var sb = new StringBuilder();
        sb.WriteLine("OpportunityName,Customer,Amount,Probability,WeightedAmount,Stage,Status,ExpectedCloseDate,AssignedTo");
        foreach (var o in opps)
        {
            sb.WriteLine($"\"{o.OpportunityName}\",\"{o.Customer.CustomerName}\",{o.Amount},{o.Probability},{o.WeightedAmount},\"{o.Stage}\",\"{o.Status}\",\"{o.ExpectedCloseDate:yyyy-MM-dd}\",\"{o.AssignedUser?.FullName}\"");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportPipelineReportCsvAsync()
    {
        var (stages, owners, total, weighted) = await GetPipelineReportAsync();
        var sb = new StringBuilder();
        sb.WriteLine("--- STAGE SUMMARY ---");
        sb.WriteLine("Stage,Count,TotalAmount,WeightedAmount");
        foreach (var s in stages)
        {
            sb.WriteLine($"\"{s.Stage}\",{s.Count},{s.TotalAmount},{s.WeightedAmount}");
        }
        sb.WriteLine($"Total,,{total},{weighted}");
        sb.WriteLine();
        sb.WriteLine("--- OWNER SUMMARY ---");
        sb.WriteLine("Owner,Count,TotalAmount,WeightedAmount");
        foreach (var o in owners)
        {
            sb.WriteLine($"\"{o.OwnerName}\",{o.OpportunityCount},{o.TotalAmount},{o.WeightedAmount}");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
