using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Security;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = $"{CrmRoles.Admin},{CrmRoles.Manager}")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserService userService, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetUsersAsync();
        return View(new UserListViewModel { Users = users });
    }

    [HttpGet]
    [Authorize(Roles = CrmRoles.Admin)]
    public IActionResult Create()
    {
        var vm = new UserCreateViewModel
        {
            RolesList = new SelectList(CrmRoles.AllRoles, CrmRoles.SalesExecutive)
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = CrmRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.RolesList = new SelectList(CrmRoles.AllRoles, vm.Role);
            return View(vm);
        }

        var (succeeded, error, _) = await _userService.CreateUserAsync(vm.FullName, vm.Email, vm.Password, vm.Role, vm.PhoneNumber);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create user.");
            vm.RolesList = new SelectList(CrmRoles.AllRoles, vm.Role);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"User '{vm.Email}' created successfully with role '{vm.Role}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = CrmRoles.Admin)]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var vm = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive,
            RolesList = new SelectList(CrmRoles.AllRoles, user.Role)
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = CrmRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel vm)
    {
        if (id != vm.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            vm.RolesList = new SelectList(CrmRoles.AllRoles, vm.Role);
            return View(vm);
        }

        var (succeeded, error) = await _userService.UpdateUserAsync(id, vm.FullName, vm.Role, vm.PhoneNumber);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update user.");
            vm.RolesList = new SelectList(CrmRoles.AllRoles, vm.Role);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"User '{vm.Email}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = CrmRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id, bool isActive)
    {
        var (succeeded, error) = await _userService.SetActiveStatusAsync(id, isActive);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to change user status.";
        }
        else
        {
            TempData["SuccessMessage"] = $"User account status updated.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = CrmRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var (succeeded, error) = await _userService.UnlockUserAsync(id);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error ?? "Failed to unlock user.";
        }
        else
        {
            TempData["SuccessMessage"] = "User account has been unlocked.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = CrmRoles.Admin)]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(new ResetPasswordViewModel
        {
            UserId = user.Id,
            Email = user.Email
        });
    }

    [HttpPost]
    [Authorize(Roles = CrmRoles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var (succeeded, error) = await _userService.ResetPasswordAsync(vm.UserId, vm.NewPassword);
        if (!succeeded)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to reset password.");
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Password for '{vm.Email}' has been reset successfully.";
        return RedirectToAction(nameof(Index));
    }
}
