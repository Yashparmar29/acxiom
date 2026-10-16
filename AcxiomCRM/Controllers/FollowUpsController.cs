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
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public FollowUpsController(
        IFollowUpService followUpService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _followUpService = followUpService;
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        FollowUpStatus? status,
        FollowUpType? type,
        string? filterType = "all",
        string? assignedTo = null,
        int page = 1)
    {
        var (followUps, total) = await _followUpService.GetFollowUpsAsync(searchTerm, status, type, filterType, assignedTo, page, 10);
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        var vm = new FollowUpListViewModel
        {
            FollowUps = followUps,
            SearchTerm = searchTerm,
            Status = status,
            Type = type,
            FilterType = filterType,
            AssignedTo = assignedTo,
            Page = page,
            PageSize = 10,
            TotalCount = total,
            UsersList = new SelectList(users, "Id", "FullName", assignedTo)
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        var vm = new FollowUpCreateViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.Today,
            AssignedTo = _currentUserService.UserId
        };

        await PopulateDropDownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        var followUp = new FollowUp
        {
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            OpportunityId = vm.OpportunityId,
            FollowUpDate = vm.FollowUpDate,
            Subject = vm.Subject.Trim(),
            FollowUpType = vm.FollowUpType,
            Remarks = vm.Remarks?.Trim(),
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, created) = await _followUpService.CreateFollowUpAsync(followUp);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to schedule follow-up.");
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Follow-up '{created!.Subject}' scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var (succeeded, error) = await _followUpService.CompleteFollowUpAsync(id, remarks);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to complete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up marked as Completed.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime followUpDate, string? remarks)
    {
        var (succeeded, error) = await _followUpService.RescheduleFollowUpAsync(id, followUpDate, remarks);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to reschedule follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Follow-up rescheduled to {followUpDate:yyyy-MM-dd}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? remarks)
    {
        var (succeeded, error) = await _followUpService.CancelFollowUpAsync(id, remarks);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to cancel follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDownsAsync(FollowUpCreateViewModel vm)
    {
        var customers = await _context.Customers.Where(c => c.Status == CustomerStatus.Active).OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => !l.IsDeleted).OrderBy(l => l.LeadName).ToListAsync();
        var opps = await _context.Opportunities.Where(o => !o.IsDeleted).OrderBy(o => o.OpportunityName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        vm.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", vm.CustomerId);
        vm.LeadsList = new SelectList(leads, "LeadId", "LeadName", vm.LeadId);
        vm.OpportunitiesList = new SelectList(opps, "OpportunityId", "OpportunityName", vm.OpportunityId);
        vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
    }
}
