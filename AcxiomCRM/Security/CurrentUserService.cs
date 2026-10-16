using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AcxiomCRM.Security;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }
    bool IsSalesExecutive { get; }
    string? PrimaryRole { get; }
    string? IpAddress { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User?.FindFirstValue("sub");

    public string? UserName =>
        User?.Identity?.Name ??
        User?.FindFirstValue(ClaimTypes.Name);

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin =>
        User?.IsInRole(CrmRoles.Admin) ?? false;

    public bool IsManager =>
        User?.IsInRole(CrmRoles.Manager) ?? false;

    public bool IsSalesExecutive =>
        User?.IsInRole(CrmRoles.SalesExecutive) ?? false;

    public string? PrimaryRole
    {
        get
        {
            if (IsAdmin) return CrmRoles.Admin;
            if (IsManager) return CrmRoles.Manager;
            if (IsSalesExecutive) return CrmRoles.SalesExecutive;
            return User?.FindFirstValue(ClaimTypes.Role);
        }
    }

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                var ip = forwarded.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip)) return ip;
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
