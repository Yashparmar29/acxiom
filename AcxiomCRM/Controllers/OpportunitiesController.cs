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
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _opportunityService = opportunityService;
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, OpportunityStage? stage, OpportunityStatus? status, string? assignedTo, int page = 1)
    {
        var (opps, total) = await _opportunityService.GetOpportunitiesAsync(searchTerm, stage, status, assignedTo, page, 10);
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        var vm = new OpportunityListViewModel
        {
            Opportunities = opps,
            SearchTerm = searchTerm,
            Stage = stage,
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
    public async Task<IActionResult> Pipeline(string? assignedTo)
    {
        var board = await _opportunityService.GetPipelineBoardAsync(assignedTo);
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        var vm = new OpportunityPipelineViewModel
        {
            Board = board,
            AssignedTo = assignedTo,
            UsersList = new SelectList(users, "Id", "FullName", assignedTo)
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id, includeRelations: true);
        if (opp == null)
        {
            return NotFound();
        }

        return View(opp);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var customers = await _context.Customers.Where(c => c.Status == CustomerStatus.Active).OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => !l.IsDeleted).OrderBy(l => l.LeadName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        var vm = new OpportunityCreateEditViewModel
        {
            CustomerId = customerId ?? (customers.FirstOrDefault()?.CustomerId ?? 0),
            LeadId = leadId,
            Probability = 10,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = _currentUserService.UserId,
            CustomersList = new SelectList(customers, "CustomerId", "CustomerName", customerId),
            LeadsList = new SelectList(leads, "LeadId", "LeadName", leadId),
            UsersList = new SelectList(users, "Id", "FullName", _currentUserService.UserId)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityCreateEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        var opp = new Opportunity
        {
            OpportunityName = vm.OpportunityName.Trim(),
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            Amount = vm.Amount,
            Stage = vm.Stage,
            Probability = vm.Probability,
            ExpectedCloseDate = vm.ExpectedCloseDate,
            Status = vm.Status,
            Source = vm.Source?.Trim(),
            Notes = vm.Notes?.Trim(),
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, created) = await _opportunityService.CreateOpportunityAsync(opp);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create opportunity.");
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Opportunity '{created!.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = created.OpportunityId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id);
        if (opp == null)
        {
            return NotFound();
        }

        if ((opp.Status == OpportunityStatus.Won || opp.Status == OpportunityStatus.Lost) && !_currentUserService.IsAdmin)
        {
            TempData["ErrorMessage"] = "Closed opportunities cannot be edited by non-administrators.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new OpportunityCreateEditViewModel
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            Source = opp.Source,
            Notes = opp.Notes,
            AssignedTo = opp.AssignedTo
        };

        await PopulateDropDownsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityCreateEditViewModel vm)
    {
        if (id != vm.OpportunityId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        var opp = new Opportunity
        {
            OpportunityId = vm.OpportunityId,
            OpportunityName = vm.OpportunityName.Trim(),
            CustomerId = vm.CustomerId,
            LeadId = vm.LeadId,
            Amount = vm.Amount,
            Stage = vm.Stage,
            Probability = vm.Probability,
            ExpectedCloseDate = vm.ExpectedCloseDate,
            Status = vm.Status,
            Source = vm.Source?.Trim(),
            Notes = vm.Notes?.Trim(),
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, updated) = await _opportunityService.UpdateOpportunityAsync(opp);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update opportunity.");
            await PopulateDropDownsAsync(vm);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Opportunity '{updated!.OpportunityName}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = updated.OpportunityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStage(int id, OpportunityStage newStage)
    {
        var (succeeded, error) = await _opportunityService.ChangeStageAsync(id, newStage);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to change opportunity stage.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Opportunity stage changed to {newStage}.";
        }

        return RedirectToAction(nameof(Pipeline));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (succeeded, error) = await _opportunityService.DeleteOpportunityAsync(id);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete opportunity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDownsAsync(OpportunityCreateEditViewModel vm)
    {
        var customers = await _context.Customers.Where(c => c.Status == CustomerStatus.Active).OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => !l.IsDeleted).OrderBy(l => l.LeadName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        vm.CustomersList = new SelectList(customers, "CustomerId", "CustomerName", vm.CustomerId);
        vm.LeadsList = new SelectList(leads, "LeadId", "LeadName", vm.LeadId);
        vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
    }
}
