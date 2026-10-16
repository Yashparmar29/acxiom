using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("pipeline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipelineReport()
    {
        var (stages, owners, grandTotal, grandWeighted) = await _reportService.GetPipelineReportAsync();
        return Ok(new
        {
            stageSummaries = stages,
            ownerSummaries = owners,
            grandTotal,
            grandWeightedTotal = grandWeighted
        });
    }
}
