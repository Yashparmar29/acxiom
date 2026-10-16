using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Security;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IAuditService auditService,
        ILogger<AuthApiController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLimit")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email) ?? await _userManager.FindByNameAsync(request.Email);
        if (user == null)
        {
            await _auditService.LogAsync("ApiLogin", "Auth", null, null, null, "Failure", $"API login failed for non-existent '{request.Email}'.");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid email or password."
            });
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync("ApiLogin", "Auth", user.Id, null, null, "Failure", $"Deactivated user '{user.Email}' attempted API login.");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Account is inactive."
            });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _auditService.LogAsync("ApiLogin", "Auth", user.Id, null, null, "Failure", $"Locked-out user '{user.Email}' attempted API login.");
            return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
            {
                Status = StatusCodes.Status423Locked,
                Title = "Account Locked",
                Detail = $"Account is locked until {user.LockoutEnd:u}. Please try again later or contact an administrator."
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiration) = _tokenService.GenerateToken(user, roles);

            await _auditService.LogAsync("ApiLogin", "Auth", user.Id, null, null, "Success", $"User '{user.Email}' logged in via API.");

            return Ok(new LoginResponseDto
            {
                Token = token,
                Expiration = expiration,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? CrmRoles.SalesExecutive
            });
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync("ApiLockout", "Auth", user.Id, null, null, "Warning", $"User '{user.Email}' locked out after API failed attempts.");
            return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
            {
                Status = StatusCodes.Status423Locked,
                Title = "Account Locked",
                Detail = "Account has been locked out due to multiple failed login attempts."
            });
        }

        await _auditService.LogAsync("ApiLogin", "Auth", user.Id, null, null, "Failure", $"Failed password attempt on API for '{user.Email}'.");
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "Invalid email or password."
        });
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _auditService.LogAsync("ApiLogout", "Auth", userId, null, null, "Success", "User logged out from API session.");
        return Ok(new { message = "Logged out successfully." });
    }
}
