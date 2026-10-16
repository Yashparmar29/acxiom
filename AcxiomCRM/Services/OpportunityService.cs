using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public interface IOpportunityService
{
    Task<(List<Opportunity> Opportunities, int TotalCount)> GetOpportunitiesAsync(string? searchTerm, OpportunityStage? stage, OpportunityStatus? status, string? assignedTo, int page = 1, int pageSize = 10);
    Task<Opportunity?> GetByIdAsync(int opportunityId, bool includeRelations = false);
    Task<Dictionary<OpportunityStage, List<Opportunity>>> GetPipelineBoardAsync(string? assignedTo = null);
    Task<(bool Succeeded, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity);
    Task<(bool Succeeded, string? ErrorMessage, Opportunity? Opportunity)> UpdateOpportunityAsync(Opportunity opportunity);
    Task<(bool Succeeded, string? ErrorMessage)> ChangeStageAsync(int opportunityId, OpportunityStage newStage);
    Task<(bool Succeeded, string? ErrorMessage)> DeleteOpportunityAsync(int opportunityId);
}

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly IOwnershipScopeHelper _scopeHelper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public OpportunityService(
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

    public async Task<(List<Opportunity> Opportunities, int TotalCount)> GetOpportunitiesAsync(string? searchTerm, OpportunityStage? stage, OpportunityStatus? status, string? assignedTo, int page = 1, int pageSize = 10)
    {
        var query = _scopeHelper.ApplyOpportunityScope(_context.Opportunities.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(o => o.OpportunityName.ToLower().Contains(term) ||
                                     o.Customer.CustomerName.ToLower().Contains(term) ||
                                     (o.Source != null && o.Source.ToLower().Contains(term)));
        }

        if (stage.HasValue)
        {
            query = query.Where(o => o.Stage == stage.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(o => o.AssignedTo == assignedTo);
        }

        var total = await query.CountAsync();
        var list = await query.Include(o => o.Customer)
                              .Include(o => o.AssignedUser)
                              .OrderByDescending(o => o.CreatedDate)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (list, total);
    }

    public async Task<Opportunity?> GetByIdAsync(int opportunityId, bool includeRelations = false)
    {
        var query = _context.Opportunities.AsQueryable();

        if (includeRelations)
        {
            query = query.Include(o => o.Customer)
                         .Include(o => o.Lead)
                         .Include(o => o.AssignedUser)
                         .Include(o => o.FollowUps.OrderByDescending(f => f.FollowUpDate));
        }
        else
        {
            query = query.Include(o => o.Customer)
                         .Include(o => o.AssignedUser);
        }

        var opportunity = await query.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId && !o.IsDeleted);
        if (opportunity == null || !_scopeHelper.CanAccessOpportunity(opportunity))
        {
            return null;
        }

        return opportunity;
    }

    public async Task<Dictionary<OpportunityStage, List<Opportunity>>> GetPipelineBoardAsync(string? assignedTo = null)
    {
        var query = _scopeHelper.ApplyOpportunityScope(_context.Opportunities.AsNoTracking())
                                .Include(o => o.Customer)
                                .Include(o => o.AssignedUser)
                                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(o => o.AssignedTo == assignedTo);
        }

        var opps = await query.ToListAsync();

        var stages = Enum.GetValues<OpportunityStage>();
        var board = new Dictionary<OpportunityStage, List<Opportunity>>();

        foreach (var stage in stages)
        {
            board[stage] = opps.Where(o => o.Stage == stage).ToList();
        }

        return board;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity)
    {
        // 1. Validation checks
        if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            return (false, "Opportunity Name is required.", null);

        if (opportunity.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.", null);

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
            return (false, "Probability must be between 0 and 100.", null);

        if (opportunity.Stage != OpportunityStage.Won && opportunity.Stage != OpportunityStage.Lost)
        {
            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past.", null);
            }
        }

        // Customer check
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == opportunity.CustomerId && c.Status == CustomerStatus.Active);
        if (!customerExists)
            return (false, "Customer does not exist or is inactive.", null);

        // Stage logic
        if (opportunity.Stage == OpportunityStage.Won)
        {
            opportunity.Status = OpportunityStatus.Won;
            opportunity.Probability = 100;
        }
        else if (opportunity.Stage == OpportunityStage.Lost)
        {
            opportunity.Status = OpportunityStatus.Lost;
            opportunity.Probability = 0;
        }
        else
        {
            opportunity.Status = OpportunityStatus.Open;
        }

        opportunity.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(opportunity.AssignedTo))
        {
            opportunity.AssignedTo = _currentUserService.UserId;
        }

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Create",
            module: "Opportunity",
            recordId: opportunity.OpportunityId.ToString(),
            oldValue: null,
            newValue: new { opportunity.OpportunityId, opportunity.OpportunityName, opportunity.Amount, opportunity.Stage, opportunity.Probability, opportunity.Status },
            result: "Success",
            details: $"Opportunity {opportunity.OpportunityName} created."
        );

        return (true, null, opportunity);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Opportunity? Opportunity)> UpdateOpportunityAsync(Opportunity opportunity)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId && !o.IsDeleted);
        if (existing == null || !_scopeHelper.CanAccessOpportunity(existing))
        {
            return (false, "Opportunity not found or unauthorized.", null);
        }

        // Closed opportunities cannot be moved back unless Admin
        if ((existing.Status == OpportunityStatus.Won || existing.Status == OpportunityStatus.Lost) && !_currentUserService.IsAdmin)
        {
            if (opportunity.Stage != existing.Stage || opportunity.Status != existing.Status)
            {
                return (false, "Closed opportunities cannot be reopened or edited by non-administrators.", null);
            }
        }

        if (opportunity.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.", null);

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
            return (false, "Probability must be between 0 and 100.", null);

        if (opportunity.Stage != OpportunityStage.Won && opportunity.Stage != OpportunityStage.Lost)
        {
            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                return (false, "Expected Close Date cannot be in the past.", null);
            }
        }

        var oldSnapshot = new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.Status, existing.ExpectedCloseDate, existing.AssignedTo };

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Source = opportunity.Source;
        existing.Notes = opportunity.Notes;

        // Stage & status rules
        existing.Stage = opportunity.Stage;
        if (opportunity.Stage == OpportunityStage.Won)
        {
            existing.Status = OpportunityStatus.Won;
            existing.Probability = 100;
        }
        else if (opportunity.Stage == OpportunityStage.Lost)
        {
            existing.Status = OpportunityStatus.Lost;
            existing.Probability = 0;
        }
        else
        {
            existing.Status = OpportunityStatus.Open;
            existing.Probability = opportunity.Probability;
        }

        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            existing.AssignedTo = opportunity.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.Status, existing.ExpectedCloseDate, existing.AssignedTo };

        await _auditService.LogAsync(
            action: "Update",
            module: "Opportunity",
            recordId: existing.OpportunityId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            result: "Success",
            details: $"Opportunity {existing.OpportunityName} updated."
        );

        return (true, null, existing);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> ChangeStageAsync(int opportunityId, OpportunityStage newStage)
    {
        var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId && !o.IsDeleted);
        if (opportunity == null || !_scopeHelper.CanAccessOpportunity(opportunity))
        {
            return (false, "Opportunity not found or unauthorized.");
        }

        if (opportunity.Stage == newStage)
        {
            return (true, null);
        }

        if ((opportunity.Status == OpportunityStatus.Won || opportunity.Status == OpportunityStatus.Lost) && !_currentUserService.IsAdmin)
        {
            return (false, "Closed opportunities cannot be moved unless you are an Administrator.");
        }

        var oldStage = opportunity.Stage;
        var oldStatus = opportunity.Status;

        opportunity.Stage = newStage;
        if (newStage == OpportunityStage.Won)
        {
            opportunity.Status = OpportunityStatus.Won;
            opportunity.Probability = 100;
        }
        else if (newStage == OpportunityStage.Lost)
        {
            opportunity.Status = OpportunityStatus.Lost;
            opportunity.Probability = 0;
        }
        else
        {
            opportunity.Status = OpportunityStatus.Open;
            opportunity.Probability = newStage switch
            {
                OpportunityStage.Qualification => 10,
                OpportunityStage.Proposal => 40,
                OpportunityStage.Negotiation => 75,
                _ => opportunity.Probability
            };
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "ChangeStage",
            module: "Opportunity",
            recordId: opportunity.OpportunityId.ToString(),
            oldValue: new { Stage = oldStage.ToString(), Status = oldStatus.ToString() },
            newValue: new { Stage = newStage.ToString(), Status = opportunity.Status.ToString(), Probability = opportunity.Probability },
            result: "Success",
            details: $"Opportunity {opportunity.OpportunityName} moved to {newStage}."
        );

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteOpportunityAsync(int opportunityId)
    {
        var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId && !o.IsDeleted);
        if (opportunity == null || !_scopeHelper.CanAccessOpportunity(opportunity))
        {
            return (false, "Opportunity not found or unauthorized.");
        }

        opportunity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Delete",
            module: "Opportunity",
            recordId: opportunity.OpportunityId.ToString(),
            oldValue: new { opportunity.OpportunityName, opportunity.Amount, opportunity.Stage },
            newValue: new { IsDeleted = true },
            result: "Success",
            details: $"Opportunity {opportunity.OpportunityName} soft-deleted."
        );

        return (true, null);
    }
}
