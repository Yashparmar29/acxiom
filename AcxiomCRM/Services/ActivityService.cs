using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public interface IActivityService
{
    Task<(List<Activity> Activities, int TotalCount)> GetActivitiesAsync(
        string? searchTerm,
        ActivityType? type,
        ActivityStatus? status,
        string? assignedTo,
        int? customerId,
        int? leadId,
        int page = 1,
        int pageSize = 10);

    Task<Activity?> GetByIdAsync(int activityId);
    Task<(bool Succeeded, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity);
    Task<(bool Succeeded, string? ErrorMessage, Activity? Activity)> UpdateActivityAsync(Activity activity);
    Task<(bool Succeeded, string? ErrorMessage)> DeleteActivityAsync(int activityId);
}

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IOwnershipScopeHelper _scopeHelper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public ActivityService(
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

    public async Task<(List<Activity> Activities, int TotalCount)> GetActivitiesAsync(
        string? searchTerm,
        ActivityType? type,
        ActivityStatus? status,
        string? assignedTo,
        int? customerId,
        int? leadId,
        int page = 1,
        int pageSize = 10)
    {
        var query = _scopeHelper.ApplyActivityScope(_context.Activities.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(a => a.Subject.ToLower().Contains(term) ||
                                     (a.Description != null && a.Description.ToLower().Contains(term)));
        }

        if (type.HasValue)
        {
            query = query.Where(a => a.ActivityType == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(a => a.AssignedTo == assignedTo);
        }

        if (customerId.HasValue)
        {
            query = query.Where(a => a.CustomerId == customerId.Value);
        }

        if (leadId.HasValue)
        {
            query = query.Where(a => a.LeadId == leadId.Value);
        }

        var total = await query.CountAsync();
        var list = await query.Include(a => a.Customer)
                              .Include(a => a.Lead)
                              .Include(a => a.AssignedUser)
                              .OrderByDescending(a => a.ActivityDate)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (list, total);
    }

    public async Task<Activity?> GetByIdAsync(int activityId)
    {
        var activity = await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(a => a.ActivityId == activityId);

        if (activity == null || !_scopeHelper.CanAccessActivity(activity))
        {
            return null;
        }

        return activity;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity)
    {
        if (string.IsNullOrWhiteSpace(activity.Subject))
            return (false, "Subject is required.", null);

        if (string.IsNullOrWhiteSpace(activity.AssignedTo))
        {
            activity.AssignedTo = _currentUserService.UserId;
        }

        activity.CreatedDate = DateTime.UtcNow;
        if (activity.ActivityDate == default)
        {
            activity.ActivityDate = DateTime.UtcNow;
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Create",
            module: "Activity",
            recordId: activity.ActivityId.ToString(),
            oldValue: null,
            newValue: new { activity.ActivityId, activity.Subject, activity.ActivityType, activity.Status, activity.ActivityDate },
            result: "Success",
            details: $"Activity '{activity.Subject}' created."
        );

        return (true, null, activity);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Activity? Activity)> UpdateActivityAsync(Activity activity)
    {
        var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activity.ActivityId);
        if (existing == null || !_scopeHelper.CanAccessActivity(existing))
        {
            return (false, "Activity not found or unauthorized.", null);
        }

        var oldSnapshot = new { existing.Subject, existing.ActivityType, existing.Status, existing.ActivityDate, existing.AssignedTo };

        existing.Subject = activity.Subject;
        existing.Description = activity.Description;
        existing.ActivityType = activity.ActivityType;
        existing.ActivityDate = activity.ActivityDate;
        existing.Status = activity.Status;
        existing.CustomerId = activity.CustomerId;
        existing.LeadId = activity.LeadId;

        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            existing.AssignedTo = activity.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = new { existing.Subject, existing.ActivityType, existing.Status, existing.ActivityDate, existing.AssignedTo };

        await _auditService.LogAsync(
            action: "Update",
            module: "Activity",
            recordId: existing.ActivityId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            result: "Success",
            details: $"Activity '{existing.Subject}' updated."
        );

        return (true, null, existing);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteActivityAsync(int activityId)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activityId);
        if (activity == null || !_scopeHelper.CanAccessActivity(activity))
        {
            return (false, "Activity not found or unauthorized.");
        }

        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Delete",
            module: "Activity",
            recordId: activityId.ToString(),
            oldValue: new { activity.Subject, activity.ActivityType },
            newValue: null,
            result: "Success",
            details: $"Activity '{activity.Subject}' deleted."
        );

        return (true, null);
    }
}
