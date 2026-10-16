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
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public LeadsController(
        ILeadService leadService,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _leadService = leadService;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, LeadStatus? status, string? assignedTo, int page = 1)
    {
        var (leads, total) = await _leadService.GetLeadsAsync(searchTerm, status, assignedTo, page, 10);
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        var vm = new LeadListViewModel
        {
            Leads = leads,
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
        var lead = await _leadService.GetByIdAsync(id, includeRelations: true);
        if (lead == null)
        {
            return NotFound();
        }

        ViewBag.AllowedStatuses = _leadService.GetAllowedNextStatuses(lead.Status);
        return View(lead);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        var vm = new LeadCreateEditViewModel
        {
            LeadCode = await _leadService.GenerateLeadCodeAsync(),
            AssignedTo = _currentUserService.UserId,
            UsersList = new SelectList(users, "Id", "FullName", _currentUserService.UserId)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadCreateEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        var lead = new Lead
        {
            LeadName = vm.LeadName.Trim(),
            Email = vm.Email?.Trim().ToLower(),
            Phone = vm.Phone?.Trim(),
            CompanyName = vm.CompanyName?.Trim(),
            Source = vm.Source,
            Status = vm.Status,
            Priority = vm.Priority,
            ExpectedValue = vm.ExpectedValue,
            Notes = vm.Notes?.Trim(),
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, created) = await _leadService.CreateLeadAsync(lead);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create lead.");
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Lead '{created!.LeadName}' ({created.LeadCode}) created successfully.";
        return RedirectToAction(nameof(Details), new { id = created.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        if ((lead.Status == LeadStatus.Converted || lead.Status == LeadStatus.Lost) && !_currentUserService.IsAdmin)
        {
            TempData["ErrorMessage"] = "Closed leads cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        var vm = new LeadCreateEditViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            Notes = lead.Notes,
            AssignedTo = lead.AssignedTo,
            UsersList = new SelectList(users, "Id", "FullName", lead.AssignedTo)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadCreateEditViewModel vm)
    {
        if (id != vm.LeadId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        var lead = new Lead
        {
            LeadId = vm.LeadId,
            LeadName = vm.LeadName.Trim(),
            Email = vm.Email?.Trim().ToLower(),
            Phone = vm.Phone?.Trim(),
            CompanyName = vm.CompanyName?.Trim(),
            Source = vm.Source,
            Status = vm.Status,
            Priority = vm.Priority,
            ExpectedValue = vm.ExpectedValue,
            Notes = vm.Notes?.Trim(),
            AssignedTo = vm.AssignedTo
        };

        var (succeeded, error, updated) = await _leadService.UpdateLeadAsync(lead);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update lead.");
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            vm.UsersList = new SelectList(users, "Id", "FullName", vm.AssignedTo);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Lead '{updated!.LeadName}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = updated.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> ChangeStatus(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        var allowed = _leadService.GetAllowedNextStatuses(lead.Status);
        if (!allowed.Any())
        {
            TempData["ErrorMessage"] = $"Lead '{lead.LeadName}' is in a terminal status ({lead.Status}) and cannot change status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new LeadChangeStatusViewModel
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            CurrentStatus = lead.Status,
            AllowedStatuses = allowed
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(LeadChangeStatusViewModel vm)
    {
        var (succeeded, error) = await _leadService.ChangeStatusAsync(vm.LeadId, vm.NewStatus, vm.Notes);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Status transition failed.");
            var lead = await _leadService.GetByIdAsync(vm.LeadId);
            if (lead != null)
            {
                vm.AllowedStatuses = _leadService.GetAllowedNextStatuses(lead.Status);
                vm.CurrentStatus = lead.Status;
                vm.LeadName = lead.LeadName;
            }
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Lead status updated to {vm.NewStatus}.";
        return RedirectToAction(nameof(Details), new { id = vm.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            return NotFound();
        }

        if (lead.Status != LeadStatus.Qualified)
        {
            TempData["ErrorMessage"] = $"Only Qualified leads can be converted. This lead is currently {lead.Status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            ExpectedValue = lead.ExpectedValue,
            CreateOpportunity = true,
            OpportunityName = $"{lead.LeadName} - Deal"
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel vm)
    {
        var (succeeded, error, customerId, oppId) = await _leadService.ConvertLeadAsync(vm.LeadId, vm.CreateOpportunity, vm.OpportunityName);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to convert lead.";
            return RedirectToAction(nameof(Details), new { id = vm.LeadId });
        }

        TempData["SuccessMessage"] = "Lead successfully converted to Customer" + (oppId.HasValue ? " and Opportunity created!" : "!");
        return RedirectToAction("Details", "Customers", new { id = customerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (succeeded, error) = await _leadService.DeleteLeadAsync(id);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete lead.";
        }
        else
        {
            TempData["SuccessMessage"] = "Lead deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
