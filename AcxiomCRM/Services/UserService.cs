using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<(bool Succeeded, string? ErrorMessage, string? UserId)> CreateUserAsync(string fullName, string email, string password, string role, string? phoneNumber);
    Task<(bool Succeeded, string? ErrorMessage)> UpdateUserAsync(string id, string fullName, string role, string? phoneNumber);
    Task<(bool Succeeded, string? ErrorMessage)> SetActiveStatusAsync(string id, bool isActive);
    Task<(bool Succeeded, string? ErrorMessage)> UnlockUserAsync(string id);
    Task<(bool Succeeded, string? ErrorMessage)> ResetPasswordAsync(string id, string newPassword);
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _userManager.Users.OrderByDescending(u => u.CreatedDate).ToListAsync();
        var dtos = new List<UserDto>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var isLocked = await _userManager.IsLockedOutAsync(u);

            dtos.Add(new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                IsLockedOut = isLocked,
                LockoutEnd = u.LockoutEnd,
                AccessFailedCount = u.AccessFailedCount,
                Role = roles.FirstOrDefault() ?? CrmRoles.SalesExecutive,
                CreatedDate = u.CreatedDate
            });
        }

        return dtos;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return null;

        var roles = await _userManager.GetRolesAsync(u);
        var isLocked = await _userManager.IsLockedOutAsync(u);

        return new UserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            IsLockedOut = isLocked,
            LockoutEnd = u.LockoutEnd,
            AccessFailedCount = u.AccessFailedCount,
            Role = roles.FirstOrDefault() ?? CrmRoles.SalesExecutive,
            CreatedDate = u.CreatedDate
        };
    }

    public async Task<(bool Succeeded, string? ErrorMessage, string? UserId)> CreateUserAsync(string fullName, string email, string password, string role, string? phoneNumber)
    {
        if (!CrmRoles.AllRoles.Contains(role))
        {
            role = CrmRoles.SalesExecutive;
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return (false, "A user with this email address already exists.", null);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)), null);
        }

        await _userManager.AddToRoleAsync(user, role);

        await _auditService.LogAsync(
            action: "CreateUser",
            module: "UserManagement",
            recordId: user.Id,
            oldValue: null,
            newValue: new { user.Id, user.Email, user.FullName, Role = role, user.IsActive },
            result: "Success",
            details: $"User '{user.Email}' created with role '{role}'."
        );

        return (true, null, user.Id);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> UpdateUserAsync(string id, string fullName, string role, string? phoneNumber)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, "User not found.");

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (CrmRoles.AllRoles.Contains(role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);
        }

        await _auditService.LogAsync(
            action: "UpdateUser",
            module: "UserManagement",
            recordId: user.Id,
            oldValue: null,
            newValue: new { user.FullName, user.PhoneNumber, Role = role },
            result: "Success",
            details: $"User '{user.Email}' updated."
        );

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> SetActiveStatusAsync(string id, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, "User not found.");

        // Prevent self-deactivation if current user is admin
        if (user.Id == _currentUserService.UserId && !isActive)
        {
            return (false, "You cannot deactivate your own account.");
        }

        var oldStatus = user.IsActive;
        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(
            action: isActive ? "ActivateUser" : "DeactivateUser",
            module: "UserManagement",
            recordId: user.Id,
            oldValue: new { IsActive = oldStatus },
            newValue: new { IsActive = isActive },
            result: "Success",
            details: $"User '{user.Email}' {(isActive ? "activated" : "deactivated")}."
        );

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> UnlockUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, "User not found.");

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            action: "UnlockUser",
            module: "UserManagement",
            recordId: user.Id,
            oldValue: null,
            newValue: new { LockoutEnd = (DateTimeOffset?)null, AccessFailedCount = 0 },
            result: "Success",
            details: $"User '{user.Email}' was unlocked by administrator."
        );

        return (true, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> ResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, "User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // Reset lockout if present
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            action: "AdminResetPassword",
            module: "UserManagement",
            recordId: user.Id,
            oldValue: null,
            newValue: null,
            result: "Success",
            details: $"Password reset for user '{user.Email}' by administrator."
        );

        return (true, null);
    }
}
