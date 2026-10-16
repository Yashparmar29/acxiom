using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IDashboardService dashboardService, ILogger<HomeController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? dateFilter = "all", DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dateFilter))
        {
            switch (dateFilter.ToLower())
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date;
                    break;
                case "week":
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    endDate = now.Date;
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = now.Date;
                    break;
            }
        }

        ViewBag.DateFilter = dateFilter;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        var metrics = await _dashboardService.GetMetricsAsync(startDate, endDate);
        return View(metrics);
    }

    [HttpGet]
    public async Task<IActionResult> GetLeadStatusChart()
    {
        var data = await _dashboardService.GetLeadStatusChartAsync();
        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetOpportunityPipelineChart()
    {
        var data = await _dashboardService.GetOpportunityPipelineChartAsync();
        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetMonthlySalesChart()
    {
        var data = await _dashboardService.GetMonthlySalesChartAsync();
        return Json(data);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
