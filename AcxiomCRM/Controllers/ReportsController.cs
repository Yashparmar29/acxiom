using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Customers(string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var (customers, total) = await _reportService.GetCustomerReportAsync(status, fromDate, toDate, page, 15);
        var vm = new CustomerReportViewModel
        {
            Customers = customers,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = 15,
            TotalCount = total
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCustomersCsv(string? status, DateTime? fromDate, DateTime? toDate)
    {
        var bytes = await _reportService.ExportCustomerReportCsvAsync(status, fromDate, toDate);
        return File(bytes, "text/csv", $"CustomerReport_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Leads(string? status, string? source, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var (leads, total) = await _reportService.GetLeadReportAsync(status, source, fromDate, toDate, page, 15);
        var vm = new LeadReportViewModel
        {
            Leads = leads,
            Status = status,
            Source = source,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = 15,
            TotalCount = total
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeadsCsv(string? status, string? source, DateTime? fromDate, DateTime? toDate)
    {
        var bytes = await _reportService.ExportLeadReportCsvAsync(status, source, fromDate, toDate);
        return File(bytes, "text/csv", $"LeadReport_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Opportunities(string? stage, string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var (opps, total) = await _reportService.GetOpportunityReportAsync(stage, status, fromDate, toDate, page, 15);
        var vm = new OpportunityReportViewModel
        {
            Opportunities = opps,
            Stage = stage,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = 15,
            TotalCount = total
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportOpportunitiesCsv(string? stage, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var bytes = await _reportService.ExportOpportunityReportCsvAsync(stage, status, fromDate, toDate);
        return File(bytes, "text/csv", $"OpportunityReport_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Pipeline()
    {
        var (stages, owners, grandTotal, grandWeighted) = await _reportService.GetPipelineReportAsync();
        var vm = new PipelineReportViewModel
        {
            StageSummaries = stages,
            OwnerSummaries = owners,
            GrandTotal = grandTotal,
            GrandWeightedTotal = grandWeighted
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPipelineCsv()
    {
        var bytes = await _reportService.ExportPipelineReportCsvAsync();
        return File(bytes, "text/csv", $"PipelineReport_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> FollowUps(string? status, string? type, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var (followUps, total) = await _reportService.GetFollowUpReportAsync(status, type, fromDate, toDate, page, 15);
        var vm = new FollowUpReportViewModel
        {
            FollowUps = followUps,
            Status = status,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = 15,
            TotalCount = total
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Conversion(DateTime? fromDate, DateTime? toDate)
    {
        var summary = await _reportService.GetConversionReportAsync(fromDate, toDate);
        var vm = new ConversionReportViewModel
        {
            Summary = summary,
            FromDate = fromDate,
            ToDate = toDate
        };
        return View(vm);
    }
}
