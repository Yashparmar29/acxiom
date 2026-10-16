using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Security;

public interface IOwnershipScopeHelper
{
    IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query);
    IQueryable<Lead> ApplyLeadScope(IQueryable<Lead> query);
    IQueryable<Opportunity> ApplyOpportunityScope(IQueryable<Opportunity> query);
    IQueryable<FollowUp> ApplyFollowUpScope(IQueryable<FollowUp> query);
    IQueryable<Activity> ApplyActivityScope(IQueryable<Activity> query);

    bool CanAccessCustomer(Customer customer);
    bool CanAccessLead(Lead lead);
    bool CanAccessOpportunity(Opportunity opportunity);
    bool CanAccessFollowUp(FollowUp followUp);
    bool CanAccessActivity(Activity activity);
}

public class OwnershipScopeHelper : IOwnershipScopeHelper
{
    private readonly ICurrentUserService _currentUserService;

    public OwnershipScopeHelper(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query)
    {
        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            return query;
        }

        var userId = _currentUserService.UserId ?? string.Empty;
        return query.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
    }

    public IQueryable<Lead> ApplyLeadScope(IQueryable<Lead> query)
    {
        var baseQuery = query.Where(l => !l.IsDeleted);
        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            return baseQuery;
        }

        var userId = _currentUserService.UserId ?? string.Empty;
        return baseQuery.Where(l => l.AssignedTo == userId || l.CreatedBy == userId);
    }

    public IQueryable<Opportunity> ApplyOpportunityScope(IQueryable<Opportunity> query)
    {
        var baseQuery = query.Where(o => !o.IsDeleted);
        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            return baseQuery;
        }

        var userId = _currentUserService.UserId ?? string.Empty;
        return baseQuery.Where(o => o.AssignedTo == userId);
    }

    public IQueryable<FollowUp> ApplyFollowUpScope(IQueryable<FollowUp> query)
    {
        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            return query;
        }

        var userId = _currentUserService.UserId ?? string.Empty;
        return query.Where(f => f.AssignedTo == userId);
    }

    public IQueryable<Activity> ApplyActivityScope(IQueryable<Activity> query)
    {
        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            return query;
        }

        var userId = _currentUserService.UserId ?? string.Empty;
        return query.Where(a => a.AssignedTo == userId);
    }

    public bool CanAccessCustomer(Customer customer)
    {
        if (_currentUserService.IsAdmin || _currentUserService.IsManager) return true;
        var userId = _currentUserService.UserId;
        return !string.IsNullOrEmpty(userId) && (customer.AssignedTo == userId || customer.CreatedBy == userId);
    }

    public bool CanAccessLead(Lead lead)
    {
        if (lead.IsDeleted) return false;
        if (_currentUserService.IsAdmin || _currentUserService.IsManager) return true;
        var userId = _currentUserService.UserId;
        return !string.IsNullOrEmpty(userId) && (lead.AssignedTo == userId || lead.CreatedBy == userId);
    }

    public bool CanAccessOpportunity(Opportunity opportunity)
    {
        if (opportunity.IsDeleted) return false;
        if (_currentUserService.IsAdmin || _currentUserService.IsManager) return true;
        var userId = _currentUserService.UserId;
        return !string.IsNullOrEmpty(userId) && opportunity.AssignedTo == userId;
    }

    public bool CanAccessFollowUp(FollowUp followUp)
    {
        if (_currentUserService.IsAdmin || _currentUserService.IsManager) return true;
        var userId = _currentUserService.UserId;
        return !string.IsNullOrEmpty(userId) && followUp.AssignedTo == userId;
    }

    public bool CanAccessActivity(Activity activity)
    {
        if (_currentUserService.IsAdmin || _currentUserService.IsManager) return true;
        var userId = _currentUserService.UserId;
        return !string.IsNullOrEmpty(userId) && activity.AssignedTo == userId;
    }
}
