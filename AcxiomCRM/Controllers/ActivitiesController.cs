using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public ActivitiesController(
        IActivityService activityService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _activityService = activityService;
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        ActivityType? type,
        ActivityStatus? status,
        string? assignedTo,
        int? customerId,
        int? leadId,
        int page = 1)
    {
        var (activities, total) = await _activityService.GetActivitiesAsync(searchTerm, type, status, assignedTo, customerId, leadId, page, 10);
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        var vm = new ActivityListViewModel
        {
            Activities = activities,
            SearchTerm = searchTerm,
            Type = type,
            Status = status,
            AssignedTo = assignedTo,
            CustomerId = customerId,
            LeadId = leadId,
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
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        return View(activity);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var vm = new ActivityCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow,
            AssignedTo = _currentUserService.UserId
        };

        await PopulateDropDownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        var activity = new Activity
        {
            ActivityType = vm.ActivityType,
            Subject = vm.Subject.Trim(),
            Description = vm.Description?.Trim(),
            ActivityDate = vm.ActivityDate,
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            Status = vm.Status,
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, created) = await _activityService.CreateActivityAsync(activity);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create activity.");
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Activity '{created!.Subject}' logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        var vm = new ActivityCreateEditViewModel
        {
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            Status = activity.Status,
            AssignedTo = activity.AssignedTo
        };

        await PopulateDropDownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityCreateEditViewModel vm)
    {
        if (id != vm.ActivityId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        var activity = new Activity
        {
            ActivityId = vm.ActivityId,
            ActivityType = vm.ActivityType,
            Subject = vm.Subject.Trim(),
            Description = vm.Description?.Trim(),
            ActivityDate = vm.ActivityDate,
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            Status = vm.Status,
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, updated) = await _activityService.UpdateActivityAsync(activity);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update activity.");
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Activity '{updated!.Subject}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (succeeded, error) = await _activityService.DeleteActivityAsync(id);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete activity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Activity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDownsAsync(ActivityCreateEditViewModel vm)
    {
        var customers = await _context.Customers.Where(c => c.Status == CustomerStatus.Active).OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => !l.IsDeleted).OrderBy(l => l.LeadName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        vm.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", vm.CustomerId);
        vm.LeadsList = new SelectList(leads, "LeadId", "LeadName", vm.LeadId);
        vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
    }
}
