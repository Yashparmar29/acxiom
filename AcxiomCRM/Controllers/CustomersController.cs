using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public CustomersController(
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _customerService = customerService;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, CustomerStatus? status, string? assignedTo, int page = 1)
    {
        var (customers, total) = await _customerService.GetCustomersAsync(searchTerm, status, assignedTo, page, 10);
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        var vm = new CustomerListViewModel
        {
            Customers = customers,
            SearchTerm = searchTerm,
            Status = status,
            AssignedTo = assignedTo,
            Page = page,
            PageSize = 10,
            TotalCount = total,
            UsersList = new SelectList(users, "Id", "FullName", assignedTo)
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetByIdAsync(id, includeRelations: true);
        if (customer == null)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        var vm = new CustomerCreateEditViewModel
        {
            CustomerCode = await _customerService.GenerateCustomerCodeAsync(),
            AssignedTo = _currentUserService.UserId,
            UsersList = new SelectList(users, "Id", "FullName", _currentUserService.UserId)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        var customer = new Customer
        {
            CustomerName = vm.CustomerName.Trim(),
            Email = vm.Email.Trim().ToLower(),
            Phone = vm.Phone.Trim(),
            CompanyName = vm.CompanyName?.Trim(),
            Address = vm.Address?.Trim(),
            City = vm.City?.Trim(),
            State = vm.State?.Trim(),
            Status = vm.Status,
            Notes = vm.Notes?.Trim(),
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, created) = await _customerService.CreateCustomerAsync(customer);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create customer.");
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Customer '{created!.CustomerName}' ({created.CustomerCode}) created successfully.";
        return RedirectToAction(nameof(Details), new { id = created.CustomerId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        var vm = new CustomerCreateEditViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            Notes = customer.Notes,
            AssignedTo = customer.AssignedTo,
            UsersList = new SelectList(users, "Id", "FullName", customer.AssignedTo)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerCreateEditViewModel vm)
    {
        if (id != vm.CustomerId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        var customer = new Customer
        {
            CustomerId = vm.CustomerId,
            CustomerName = vm.CustomerName.Trim(),
            Email = vm.Email.Trim().ToLower(),
            Phone = vm.Phone.Trim(),
            CompanyName = vm.CompanyName?.Trim(),
            Address = vm.Address?.Trim(),
            City = vm.City?.Trim(),
            State = vm.State?.Trim(),
            Status = vm.Status,
            Notes = vm.Notes?.Trim(),
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, updated) = await _customerService.UpdateCustomerAsync(customer);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update customer.");
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Customer '{updated!.CustomerName}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = updated.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (succeeded, error) = await _customerService.DeactivateCustomerAsync(id);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to deactivate customer.";
        }
        else
        {
            TempData["SuccessMessage"] = "Customer has been deactivated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CheckEmailAvailable(string email, int? customerId)
    {
        var isAvailable = await _customerService.IsEmailAvailableAsync(email, customerId);
        return Json(isAvailable);
    }

    [HttpGet]
    public async Task<IActionResult> CheckPhoneAvailable(string phone, int? customerId)
    {
        var isAvailable = await _customerService.IsPhoneAvailableAsync(phone, customerId);
        return Json(isAvailable);
    }
}
