using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Security;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = $"{CrmRoles.Admin},{CrmRoles.Manager}")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var (logs, total) = await _auditService.GetAuditLogsAsync(userFilter, moduleFilter, actionFilter, fromDate, toDate, page, 20);

        var vm = new AuditReportViewModel
        {
            Logs = logs,
            UserFilter = userFilter,
            ModuleFilter = moduleFilter,
            ActionFilter = actionFilter,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = 20,
            TotalCount = total
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var log = await _auditService.GetByIdAsync(id);
        if (log == null)
        {
            return NotFound();
        }

        return View(log);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? fromDate, DateTime? toDate)
    {
        var csvBytes = await _auditService.ExportCsvAsync(userFilter, moduleFilter, actionFilter, fromDate, toDate);
        return File(csvBytes, "text/csv", $"AuditLogs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }
}
