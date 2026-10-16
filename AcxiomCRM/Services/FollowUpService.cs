using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public interface IFollowUpService
{
    Task<(List<FollowUp> FollowUps, int TotalCount)> GetFollowUpsAsync(
        string? searchTerm,
        FollowUpStatus? status,
        FollowUpType? type,
        string? filterType, // "all", "pending", "today", "upcoming", "overdue"
        string? assignedTo,
        int page = 1,
        int pageSize = 10);

    Task<FollowUp?> GetByIdAsync(int followUpId);
    Task<(bool Succeeded, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp);
    Task<(bool Succeeded, string? ErrorMessage)> CompleteFollowUpAsync(int followUpId, string? remarks);
    Task<(bool Succeeded, string? ErrorMessage)> RescheduleFollowUpAsync(int followUpId, DateTime newDate, string? remarks);
    Task<(bool Succeeded, string? ErrorMessage)> CancelFollowUpAsync(int followUpId, string? remarks);
    Task MarkOverdueFollowUpsAsMissedAsync();
}

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly IOwnershipScopeHelper _scopeHelper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public FollowUpService(
        ApplicationDbContext context,
        IOwnershipScopeHelper scopeHelper,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _scopeHelper = scopeHelper;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task MarkOverdueFollowUpsAsMissedAsync()
    {
        var now = DateTime.UtcNow;
        var overdue = await _context.FollowUps
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < now.Date)
            .ToListAsync();

        if (overdue.Any())
        {
            foreach (var f in overdue)
            {
                f.Status = FollowUpStatus.Missed;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<(List<FollowUp> FollowUps, int TotalCount)> GetFollowUpsAsync(
        string? searchTerm,
        FollowUpStatus? status,
        FollowUpType? type,
        string? filterType,
        string? assignedTo,
        int page = 1,
        int pageSize = 10)
    {
        // Mark overdue planned items first
        await MarkOverdueFollowUpsAsMissedAsync();

        var query = _scopeHelper.ApplyFollowUpScope(_context.FollowUps.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(f => f.Subject.ToLower().Contains(term) ||
                                     (f.Remarks != null && f.Remarks.ToLower().Contains(term)) ||
                                     (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                                     (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)) ||
                                     (f.Opportunity != null && f.Opportunity.OpportunityName.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(f => f.FollowUpType == type.Value);
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        if (!string.IsNullOrWhiteSpace(filterType))
        {
            switch (filterType.ToLower())
            {
                case "pending":
                    query = query.Where(f => f.Status == FollowUpStatus.Planned);
                    break;
                case "today":
                    query = query.Where(f => f.FollowUpDate >= today && f.FollowUpDate < tomorrow);
                    break;
                case "upcoming":
                    query = query.Where(f => f.FollowUpDate >= tomorrow && f.Status == FollowUpStatus.Planned);
                    break;
                case "overdue":
                    query = query.Where(f => (f.Status == FollowUpStatus.Missed) || (f.Status == FollowUpStatus.Planned && f.FollowUpDate < today));
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(f => f.AssignedTo == assignedTo);
        }

        var total = await query.CountAsync();
        var list = await query.Include(f => f.Customer)
                              .Include(f => f.Lead)
                              .Include(f => f.Opportunity)
                              .Include(f => f.AssignedUser)
                              .OrderBy(f => f.FollowUpDate)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (list, total);
    }

    public async Task<FollowUp?> GetByIdAsync(int followUpId)
    {
        var followUp = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser)
            .FirstOrDefaultAsync(f => f.FollowUpId == followUpId);

        if (followUp == null || !_scopeHelper.CanAccessFollowUp(followUp))
        {
            return null;
        }

        return followUp;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp)
    {
        if (string.IsNullOrWhiteSpace(followUp.Subject))
            return (false, "Subject is required.", null);

        if (!followUp.CustomerId.HasValue && !followUp.LeadId.HasValue && !followUp.OpportunityId.HasValue)
            return (false, "At least one related entity (Customer, Lead, or Opportunity) must be selected.", null);

        if (followUp.FollowUpDate.Date < DateTime.UtcNow.Date && followUp.FollowUpDate.Date < DateTime.Today)
            return (false, "Follow-up date cannot be earlier than today.", null);

        followUp.CreatedDate = DateTime.UtcNow;
        followUp.Status = FollowUpStatus.Planned;

        if (string.IsNullOrWhiteSpace(followUp.AssignedTo))
        {
            followUp.AssignedTo = _currentUserService.UserId;
        }

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Schedule",
            module: "FollowUp",
            recordId: followUp.FollowUpId.ToString(),
            oldValue: null,
            newValue: new { followUp.FollowUpId, followUp.Subject, followUp.FollowUpDate, followUp.FollowUpType, followUp.Status },
            result: "Success",
            details: $"Follow-Up '{followUp.Subject}' scheduled for {followUp.FollowUpDate:yyyy-MM-dd}."
        );

        return (true, null, followUp);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> CompleteFollowUpAsync(int followUpId, string? remarks)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
        if (followUp == null || !_scopeHelper.CanAccessFollowUp(followUp))
        {
            return (false, "Follow-up not found or unauthorized.");
        }

        var oldStatus = followUp.Status;
        followUp.Status = FollowUpStatus.Completed;
        followUp.CompletedDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? remarks
                : $"{followUp.Remarks}\n[Completed on {DateTime.UtcNow:yyyy-MM-dd HH:mm}]: {remarks}";
        }

        // Also automatically create an Activity for the completion
        var activity = new Activity
        {
            ActivityType = (ActivityType)(int)followUp.FollowUpType,
            Subject = $"Completed Follow-Up: {followUp.Subject}",
            Description = remarks ?? followUp.Remarks,
            ActivityDate = DateTime.UtcNow,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            AssignedTo = followUp.AssignedTo,
            Status = ActivityStatus.Completed,
            CreatedDate = DateTime.UtcNow
        };
        _context.Activities.Add(activity);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Complete",
            module: "FollowUp",
            recordId: followUp.FollowUpId.ToString(),
            oldValue: new { Status = oldStatus.ToString() },
            newValue: new { Status = FollowUpStatus.Completed.ToString(), followUp.CompletedDate },
            result: "Success",
            details: $"Follow-Up '{followUp.Subject}' completed."
        );

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> RescheduleFollowUpAsync(int followUpId, DateTime newDate, string? remarks)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
        if (followUp == null || !_scopeHelper.CanAccessFollowUp(followUp))
        {
            return (false, "Follow-up not found or unauthorized.");
        }

        if (newDate.Date < DateTime.UtcNow.Date && newDate.Date < DateTime.Today)
        {
            return (false, "Follow-up date cannot be earlier than today.");
        }

        var oldDate = followUp.FollowUpDate;
        followUp.FollowUpDate = newDate;
        followUp.Status = FollowUpStatus.Planned;

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? remarks
                : $"{followUp.Remarks}\n[Rescheduled to {newDate:yyyy-MM-dd}]: {remarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Reschedule",
            module: "FollowUp",
            recordId: followUp.FollowUpId.ToString(),
            oldValue: new { FollowUpDate = oldDate },
            newValue: new { FollowUpDate = newDate, Status = FollowUpStatus.Planned.ToString() },
            result: "Success",
            details: $"Follow-Up '{followUp.Subject}' rescheduled from {oldDate:yyyy-MM-dd} to {newDate:yyyy-MM-dd}."
        );

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> CancelFollowUpAsync(int followUpId, string? remarks)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
        if (followUp == null || !_scopeHelper.CanAccessFollowUp(followUp))
        {
            return (false, "Follow-up not found or unauthorized.");
        }

        var oldStatus = followUp.Status;
        followUp.Status = FollowUpStatus.Cancelled;

        if (!string.IsNullOrWhiteSpace(remarks))
        {
            followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                ? remarks
                : $"{followUp.Remarks}\n[Cancelled]: {remarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Cancel",
            module: "FollowUp",
            recordId: followUp.FollowUpId.ToString(),
            oldValue: new { Status = oldStatus.ToString() },
            newValue: new { Status = FollowUpStatus.Cancelled.ToString() },
            result: "Success",
            details: $"Follow-Up '{followUp.Subject}' cancelled."
        );

        return (true, null);
    }
}
