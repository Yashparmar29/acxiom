using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public interface ILeadService
{
    Task<(List<Lead> Leads, int TotalCount)> GetLeadsAsync(string? searchTerm, LeadStatus? status, string? assignedTo, int page = 1, int pageSize = 10);
    Task<Lead?> GetByIdAsync(int leadId, bool includeRelations = false);
    Task<(bool Succeeded, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead);
    Task<(bool Succeeded, string? ErrorMessage, Lead? Lead)> UpdateLeadAsync(Lead lead);
    Task<(bool Succeeded, string? ErrorMessage)> ChangeStatusAsync(int leadId, LeadStatus newStatus, string? notes = null);
    Task<(bool Succeeded, string? ErrorMessage, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(int leadId, bool createOpportunity = true, string? opportunityName = null);
    Task<(bool Succeeded, string? ErrorMessage)> DeleteLeadAsync(int leadId);
    IReadOnlyList<LeadStatus> GetAllowedNextStatuses(LeadStatus currentStatus);
    bool CanTransition(LeadStatus currentStatus, LeadStatus targetStatus);
    Task<string> GenerateLeadCodeAsync();
}

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly IOwnershipScopeHelper _scopeHelper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly ICustomerService _customerService;

    public LeadService(
        ApplicationDbContext context,
        IOwnershipScopeHelper scopeHelper,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        ICustomerService customerService)
    {
        _context = context;
        _scopeHelper = scopeHelper;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _customerService = customerService;
    }

    public IReadOnlyList<LeadStatus> GetAllowedNextStatuses(LeadStatus currentStatus)
    {
        return currentStatus switch
        {
            LeadStatus.New => new[] { LeadStatus.Contacted, LeadStatus.Lost },
            LeadStatus.Contacted => new[] { LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Lost },
            LeadStatus.Qualified => new[] { LeadStatus.Converted, LeadStatus.Lost },
            LeadStatus.Unqualified => new[] { LeadStatus.Contacted, LeadStatus.Lost },
            LeadStatus.Converted => Array.Empty<LeadStatus>(),
            LeadStatus.Lost => Array.Empty<LeadStatus>(),
            _ => Array.Empty<LeadStatus>()
        };
    }

    public bool CanTransition(LeadStatus currentStatus, LeadStatus targetStatus)
    {
        if (currentStatus == targetStatus) return true;
        var allowed = GetAllowedNextStatuses(currentStatus);
        return allowed.Contains(targetStatus);
    }

    public async Task<string> GenerateLeadCodeAsync()
    {
        var maxId = await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 0;
        return $"LEAD-{(maxId + 1):D4}";
    }

    public async Task<(List<Lead> Leads, int TotalCount)> GetLeadsAsync(string? searchTerm, LeadStatus? status, string? assignedTo, int page = 1, int pageSize = 10)
    {
        var query = _scopeHelper.ApplyLeadScope(_context.Leads.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                     l.LeadCode.ToLower().Contains(term) ||
                                     (l.Email != null && l.Email.ToLower().Contains(term)) ||
                                     (l.Phone != null && l.Phone.Contains(term)) ||
                                     (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(l => l.AssignedTo == assignedTo);
        }

        var total = await query.CountAsync();
        var list = await query.Include(l => l.AssignedUser)
                              .Include(l => l.ConvertedCustomer)
                              .OrderByDescending(l => l.CreatedDate)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (list, total);
    }

    public async Task<Lead?> GetByIdAsync(int leadId, bool includeRelations = false)
    {
        var query = _context.Leads.AsQueryable();

        if (includeRelations)
        {
            query = query.Include(l => l.AssignedUser)
                         .Include(l => l.CreatedByUser)
                         .Include(l => l.ConvertedCustomer)
                         .Include(l => l.Opportunities.Where(o => !o.IsDeleted))
                         .Include(l => l.FollowUps.OrderByDescending(f => f.FollowUpDate))
                         .Include(l => l.Activities.OrderByDescending(a => a.ActivityDate));
        }
        else
        {
            query = query.Include(l => l.AssignedUser)
                         .Include(l => l.ConvertedCustomer);
        }

        var lead = await query.FirstOrDefaultAsync(l => l.LeadId == leadId && !l.IsDeleted);
        if (lead == null || !_scopeHelper.CanAccessLead(lead))
        {
            return null;
        }

        return lead;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead)
    {
        if (string.IsNullOrWhiteSpace(lead.LeadName))
            return (false, "Lead Name is required.", null);

        if (lead.ExpectedValue < 0 || lead.ExpectedValue > 100000000m)
            return (false, "Expected Value must be between 0 and 100,000,000.", null);

        if (string.IsNullOrWhiteSpace(lead.LeadCode))
        {
            lead.LeadCode = await GenerateLeadCodeAsync();
        }

        lead.CreatedDate = DateTime.UtcNow;
        lead.CreatedBy = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(lead.AssignedTo))
        {
            lead.AssignedTo = _currentUserService.UserId;
        }

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Create",
            module: "Lead",
            recordId: lead.LeadId.ToString(),
            oldValue: null,
            newValue: new { lead.LeadId, lead.LeadCode, lead.LeadName, lead.Status, lead.Priority, lead.ExpectedValue },
            result: "Success",
            details: $"Lead {lead.LeadCode} - {lead.LeadName} created."
        );

        return (true, null, lead);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Lead? Lead)> UpdateLeadAsync(Lead lead)
    {
        var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId && !l.IsDeleted);
        if (existing == null || !_scopeHelper.CanAccessLead(existing))
        {
            return (false, "Lead not found or unauthorized.", null);
        }

        if (existing.Status == LeadStatus.Converted || existing.Status == LeadStatus.Lost)
        {
            if (!_currentUserService.IsAdmin)
            {
                return (false, "Converted or Lost leads cannot be edited.", null);
            }
        }

        if (lead.ExpectedValue < 0 || lead.ExpectedValue > 100000000m)
            return (false, "Expected Value must be between 0 and 100,000,000.", null);

        var oldSnapshot = new { existing.LeadName, existing.Email, existing.Phone, existing.CompanyName, existing.Priority, existing.ExpectedValue, existing.AssignedTo };

        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;
        existing.Notes = lead.Notes;

        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            existing.AssignedTo = lead.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = new { existing.LeadName, existing.Email, existing.Phone, existing.CompanyName, existing.Priority, existing.ExpectedValue, existing.AssignedTo };

        await _auditService.LogAsync(
            action: "Update",
            module: "Lead",
            recordId: existing.LeadId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            result: "Success",
            details: $"Lead {existing.LeadCode} updated."
        );

        return (true, null, existing);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> ChangeStatusAsync(int leadId, LeadStatus newStatus, string? notes = null)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId && !l.IsDeleted);
        if (lead == null || !_scopeHelper.CanAccessLead(lead))
        {
            return (false, "Lead not found or unauthorized.");
        }

        if (lead.Status == newStatus)
        {
            return (true, null);
        }

        if (newStatus == LeadStatus.Converted)
        {
            return (false, "To convert a lead, use the Convert Lead action.");
        }

        if (!CanTransition(lead.Status, newStatus))
        {
            return (false, $"Invalid transition from {lead.Status} to {newStatus}. Allowed transitions: {string.Join(", ", GetAllowedNextStatuses(lead.Status))}.");
        }

        var oldStatus = lead.Status;
        lead.Status = newStatus;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            lead.Notes = string.IsNullOrWhiteSpace(lead.Notes)
                ? $"[{DateTime.UtcNow:yyyy-MM-dd}] Status changed to {newStatus}: {notes}"
                : $"{lead.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd}] Status changed to {newStatus}: {notes}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "ChangeStatus",
            module: "Lead",
            recordId: lead.LeadId.ToString(),
            oldValue: new { Status = oldStatus.ToString() },
            newValue: new { Status = newStatus.ToString() },
            result: "Success",
            details: $"Lead {lead.LeadCode} status changed from {oldStatus} to {newStatus}."
        );

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(int leadId, bool createOpportunity = true, string? opportunityName = null)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId && !l.IsDeleted);
        if (lead == null || !_scopeHelper.CanAccessLead(lead))
        {
            return (false, "Lead not found or unauthorized.", null, null);
        }

        if (lead.Status != LeadStatus.Qualified)
        {
            return (false, $"Only Qualified leads can be converted. Current status is {lead.Status}.", null, null);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check or Create Customer
            Customer? customer = null;
            if (!string.IsNullOrWhiteSpace(lead.Email) || !string.IsNullOrWhiteSpace(lead.Phone))
            {
                customer = await _customerService.GetByEmailOrPhoneAsync(lead.Email ?? string.Empty, lead.Phone ?? string.Empty);
            }

            if (customer == null)
            {
                // Generate a dummy valid phone/email if missing on lead
                var email = !string.IsNullOrWhiteSpace(lead.Email) ? lead.Email : $"lead_{lead.LeadId}_{DateTime.UtcNow.Ticks}@placeholder.crm";
                var phone = !string.IsNullOrWhiteSpace(lead.Phone) ? lead.Phone : "9876543210";

                customer = new Customer
                {
                    CustomerCode = await _customerService.GenerateCustomerCodeAsync(),
                    CustomerName = lead.LeadName,
                    Email = email,
                    Phone = phone,
                    CompanyName = lead.CompanyName,
                    Status = CustomerStatus.Active,
                    Notes = $"Converted from Lead {lead.LeadCode}",
                    AssignedTo = lead.AssignedTo ?? _currentUserService.UserId,
                    CreatedBy = _currentUserService.UserId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            // 2. Optionally Create Opportunity
            Opportunity? opportunity = null;
            if (createOpportunity)
            {
                opportunity = new Opportunity
                {
                    OpportunityName = !string.IsNullOrWhiteSpace(opportunityName) ? opportunityName : $"{lead.LeadName} - Converted Deal",
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 1000m,
                    Stage = OpportunityStage.Qualification,
                    Probability = 10,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                    Status = OpportunityStatus.Open,
                    Source = lead.Source.ToString(),
                    Notes = $"Created upon conversion of Lead {lead.LeadCode}.",
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = lead.AssignedTo ?? _currentUserService.UserId
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();
            }

            // 3. Update Lead
            lead.Status = LeadStatus.Converted;
            lead.ConvertedCustomerId = customer.CustomerId;
            lead.ConvertedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogAsync(
                action: "Convert",
                module: "Lead",
                recordId: lead.LeadId.ToString(),
                oldValue: new { Status = LeadStatus.Qualified.ToString() },
                newValue: new { Status = LeadStatus.Converted.ToString(), CustomerId = customer.CustomerId, OpportunityId = opportunity?.OpportunityId },
                result: "Success",
                details: $"Lead {lead.LeadCode} converted to Customer {customer.CustomerCode}."
            );

            return (true, null, customer.CustomerId, opportunity?.OpportunityId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lead conversion failed: {ex.Message}", null, null);
        }
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteLeadAsync(int leadId)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId && !l.IsDeleted);
        if (lead == null || !_scopeHelper.CanAccessLead(lead))
        {
            return (false, "Lead not found or unauthorized.");
        }

        lead.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Delete",
            module: "Lead",
            recordId: lead.LeadId.ToString(),
            oldValue: new { lead.LeadCode, lead.LeadName, lead.Status },
            newValue: new { IsDeleted = true },
            result: "Success",
            details: $"Lead {lead.LeadCode} soft-deleted."
        );

        return (true, null);
    }
}
