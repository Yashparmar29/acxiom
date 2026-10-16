using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public class DashboardMetrics
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int PendingFollowUps { get; set; }

    // Admin extras
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
    public int RecentSecurityEvents { get; set; }

    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<Activity> RecentActivities { get; set; } = new();
}

public class ChartData
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Data { get; set; } = new();
    public List<string>? BackgroundColors { get; set; }
}

public interface IDashboardService
{
    Task<DashboardMetrics> GetMetricsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<ChartData> GetLeadStatusChartAsync();
    Task<ChartData> GetOpportunityPipelineChartAsync();
    Task<ChartData> GetMonthlySalesChartAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IOwnershipScopeHelper _scopeHelper;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(
        ApplicationDbContext context,
        IOwnershipScopeHelper scopeHelper,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _scopeHelper = scopeHelper;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var custQuery = _scopeHelper.ApplyCustomerScope(_context.Customers.AsNoTracking());
        var leadQuery = _scopeHelper.ApplyLeadScope(_context.Leads.AsNoTracking());
        var oppQuery = _scopeHelper.ApplyOpportunityScope(_context.Opportunities.AsNoTracking());
        var followQuery = _scopeHelper.ApplyFollowUpScope(_context.FollowUps.AsNoTracking());
        var actQuery = _scopeHelper.ApplyActivityScope(_context.Activities.AsNoTracking());

        if (startDate.HasValue)
        {
            custQuery = custQuery.Where(c => c.CreatedDate >= startDate.Value);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);
            custQuery = custQuery.Where(c => c.CreatedDate < end);
            leadQuery = leadQuery.Where(l => l.CreatedDate < end);
            oppQuery = oppQuery.Where(o => o.CreatedDate < end);
        }

        var metrics = new DashboardMetrics
        {
            TotalCustomers = await custQuery.CountAsync(),
            TotalLeads = await leadQuery.CountAsync(),
            OpenLeads = await leadQuery.CountAsync(l => l.Status == LeadStatus.New || l.Status == LeadStatus.Contacted || l.Status == LeadStatus.Qualified),
            TotalOpportunities = await oppQuery.CountAsync(),
            OpenOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Open),
            WonOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Won),
            LostOpportunities = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Lost),
            PendingFollowUps = await followQuery.CountAsync(f => f.Status == FollowUpStatus.Planned)
        };

        var openOpps = await oppQuery.Where(o => o.Status == OpportunityStatus.Open).ToListAsync();
        metrics.TotalPipelineValue = openOpps.Sum(o => o.Amount);
        metrics.WeightedPipelineValue = openOpps.Sum(o => o.WeightedAmount);

        if (_currentUserService.IsAdmin)
        {
            metrics.TotalUsers = await _context.Users.CountAsync();
            metrics.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            metrics.LockedUsers = await _context.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
            metrics.RecentSecurityEvents = await _context.AuditLogs.CountAsync(a => a.CreatedDate >= DateTime.UtcNow.AddDays(-7));
        }

        // Upcoming followups
        metrics.UpcomingFollowUps = await followQuery
            .Where(f => f.Status == FollowUpStatus.Planned)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedUser)
            .ToListAsync();

        // Recent activities
        metrics.RecentActivities = await actQuery
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .ToListAsync();

        return metrics;
    }

    public async Task<ChartData> GetLeadStatusChartAsync()
    {
        var leads = await _scopeHelper.ApplyLeadScope(_context.Leads.AsNoTracking()).ToListAsync();

        var statuses = new[] { LeadStatus.New, LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Converted, LeadStatus.Lost };
        var chart = new ChartData
        {
            Labels = statuses.Select(s => s.ToString()).ToList(),
            Data = statuses.Select(s => (decimal)leads.Count(l => l.Status == s)).ToList(),
            BackgroundColors = new List<string> { "#3b82f6", "#06b6d4", "#10b981", "#f59e0b", "#6366f1", "#ef4444" }
        };

        return chart;
    }

    public async Task<ChartData> GetOpportunityPipelineChartAsync()
    {
        var opps = await _scopeHelper.ApplyOpportunityScope(_context.Opportunities.AsNoTracking()).ToListAsync();

        var stages = new[] { OpportunityStage.Qualification, OpportunityStage.Proposal, OpportunityStage.Negotiation, OpportunityStage.Won, OpportunityStage.Lost };
        var chart = new ChartData
        {
            Labels = stages.Select(s => s.ToString()).ToList(),
            Data = stages.Select(s => opps.Where(o => o.Stage == s).Sum(o => o.Amount)).ToList(),
            BackgroundColors = new List<string> { "#60a5fa", "#a78bfa", "#f59e0b", "#10b981", "#ef4444" }
        };

        return chart;
    }

    public async Task<ChartData> GetMonthlySalesChartAsync()
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

        var wonOpps = await _scopeHelper.ApplyOpportunityScope(_context.Opportunities.AsNoTracking())
            .Where(o => o.Status == OpportunityStatus.Won && o.CreatedDate >= start)
            .ToListAsync();

        var labels = new List<string>();
        var data = new List<decimal>();

        for (int i = 0; i < 12; i++)
        {
            var monthDate = start.AddMonths(i);
            var monthName = monthDate.ToString("MMM yyyy");
            labels.Add(monthName);

            var totalWon = wonOpps
                .Where(o => o.CreatedDate.Year == monthDate.Year && o.CreatedDate.Month == monthDate.Month)
                .Sum(o => o.Amount);

            data.Add(totalWon);
        }

        return new ChartData
        {
            Labels = labels,
            Data = data,
            BackgroundColors = new List<string> { "#10b981" }
        };
    }
}
