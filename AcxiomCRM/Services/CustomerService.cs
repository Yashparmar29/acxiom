using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public interface ICustomerService
{
    Task<(List<Customer> Customers, int TotalCount)> GetCustomersAsync(string? searchTerm, CustomerStatus? status, string? assignedTo, int page = 1, int pageSize = 10);
    Task<Customer?> GetByIdAsync(int customerId, bool includeRelations = false);
    Task<Customer?> GetByEmailOrPhoneAsync(string email, string phone);
    Task<(bool Succeeded, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer);
    Task<(bool Succeeded, string? ErrorMessage, Customer? Customer)> UpdateCustomerAsync(Customer customer);
    Task<(bool Succeeded, string? ErrorMessage)> DeactivateCustomerAsync(int customerId);
    Task<bool> IsEmailAvailableAsync(string email, int? customerId = null);
    Task<bool> IsPhoneAvailableAsync(string phone, int? customerId = null);
    Task<string> GenerateCustomerCodeAsync();
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IOwnershipScopeHelper _scopeHelper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public CustomerService(
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

    public async Task<(List<Customer> Customers, int TotalCount)> GetCustomersAsync(string? searchTerm, CustomerStatus? status, string? assignedTo, int page = 1, int pageSize = 10)
    {
        var query = _scopeHelper.ApplyCustomerScope(_context.Customers.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                     c.CustomerCode.ToLower().Contains(term) ||
                                     c.Email.ToLower().Contains(term) ||
                                     c.Phone.ToLower().Contains(term) ||
                                     (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(c => c.AssignedTo == assignedTo);
        }

        var total = await query.CountAsync();
        var list = await query.Include(c => c.AssignedUser)
                              .OrderByDescending(c => c.CreatedDate)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (list, total);
    }

    public async Task<Customer?> GetByIdAsync(int customerId, bool includeRelations = false)
    {
        var query = _context.Customers.AsQueryable();

        if (includeRelations)
        {
            query = query.Include(c => c.AssignedUser)
                         .Include(c => c.CreatedByUser)
                         .Include(c => c.Opportunities.Where(o => !o.IsDeleted))
                         .Include(c => c.FollowUps.OrderByDescending(f => f.FollowUpDate))
                         .Include(c => c.Activities.OrderByDescending(a => a.ActivityDate))
                         .Include(c => c.Leads.Where(l => !l.IsDeleted));
        }
        else
        {
            query = query.Include(c => c.AssignedUser);
        }

        var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer == null || !_scopeHelper.CanAccessCustomer(customer))
        {
            return null;
        }

        return customer;
    }

    public async Task<Customer?> GetByEmailOrPhoneAsync(string email, string phone)
    {
        var cleanEmail = email.Trim().ToLower();
        var cleanPhone = phone.Trim();
        return await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == cleanEmail || c.Phone == cleanPhone);
    }

    public async Task<string> GenerateCustomerCodeAsync()
    {
        var maxId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
        return $"CUST-{(maxId + 1):D4}";
    }

    public async Task<bool> IsEmailAvailableAsync(string email, int? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var clean = email.Trim().ToLower();
        return !await _context.Customers.AnyAsync(c => c.Email.ToLower() == clean && (!customerId.HasValue || c.CustomerId != customerId.Value));
    }

    public async Task<bool> IsPhoneAvailableAsync(string phone, int? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true;
        var clean = phone.Trim();
        return !await _context.Customers.AnyAsync(c => c.Phone == clean && (!customerId.HasValue || c.CustomerId != customerId.Value));
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer)
    {
        // 1. Validation checks
        if (string.IsNullOrWhiteSpace(customer.CustomerName))
            return (false, "Customer Name is required.", null);

        if (string.IsNullOrWhiteSpace(customer.Email))
            return (false, "Email is required.", null);

        if (string.IsNullOrWhiteSpace(customer.Phone))
            return (false, "Phone is required.", null);

        // 2. Uniqueness checks
        if (!await IsEmailAvailableAsync(customer.Email))
            return (false, "A customer with this email address already exists.", null);

        if (!await IsPhoneAvailableAsync(customer.Phone))
            return (false, "A customer with this phone number already exists.", null);

        // Duplicate name + company check
        if (!string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            var match = await _context.Customers.AnyAsync(c =>
                c.CustomerName.ToLower() == customer.CustomerName.Trim().ToLower() &&
                c.CompanyName != null && c.CompanyName.ToLower() == customer.CompanyName.Trim().ToLower());
            if (match)
            {
                return (false, "A customer with the same name and company already exists.", null);
            }
        }

        // 3. Default fields
        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
        {
            customer.CustomerCode = await GenerateCustomerCodeAsync();
        }

        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(customer.AssignedTo))
        {
            customer.AssignedTo = _currentUserService.UserId;
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Create",
            module: "Customer",
            recordId: customer.CustomerId.ToString(),
            oldValue: null,
            newValue: new { customer.CustomerId, customer.CustomerCode, customer.CustomerName, customer.Email, customer.Phone, customer.Status },
            result: "Success",
            details: $"Customer {customer.CustomerCode} - {customer.CustomerName} created."
        );

        return (true, null, customer);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Customer? Customer)> UpdateCustomerAsync(Customer customer)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
        if (existing == null || !_scopeHelper.CanAccessCustomer(existing))
        {
            return (false, "Customer not found or unauthorized.", null);
        }

        // Uniqueness checks
        if (!await IsEmailAvailableAsync(customer.Email, customer.CustomerId))
            return (false, "A customer with this email address already exists.", null);

        if (!await IsPhoneAvailableAsync(customer.Phone, customer.CustomerId))
            return (false, "A customer with this phone number already exists.", null);

        var oldSnapshot = new { existing.CustomerName, existing.Email, existing.Phone, existing.CompanyName, existing.Status, existing.AssignedTo };

        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        existing.Notes = customer.Notes;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = _currentUserService.UserId;

        // Only Admin or Manager can reassign customer, otherwise keep or self-assign
        if (_currentUserService.IsAdmin || _currentUserService.IsManager)
        {
            existing.AssignedTo = customer.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newSnapshot = new { existing.CustomerName, existing.Email, existing.Phone, existing.CompanyName, existing.Status, existing.AssignedTo };

        await _auditService.LogAsync(
            action: "Update",
            module: "Customer",
            recordId: existing.CustomerId.ToString(),
            oldValue: oldSnapshot,
            newValue: newSnapshot,
            result: "Success",
            details: $"Customer {existing.CustomerCode} updated."
        );

        return (true, null, existing);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeactivateCustomerAsync(int customerId)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer == null || !_scopeHelper.CanAccessCustomer(customer))
        {
            return (false, "Customer not found or unauthorized.");
        }

        var oldStatus = customer.Status;
        customer.Status = CustomerStatus.Inactive;
        customer.ModifiedDate = DateTime.UtcNow;
        customer.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Deactivate",
            module: "Customer",
            recordId: customer.CustomerId.ToString(),
            oldValue: new { Status = oldStatus.ToString() },
            newValue: new { Status = CustomerStatus.Inactive.ToString() },
            result: "Success",
            details: $"Customer {customer.CustomerCode} deactivated (soft delete)."
        );

        return (true, null);
    }
}
