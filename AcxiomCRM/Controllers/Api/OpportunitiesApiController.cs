using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<OpportunityReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, [FromQuery] OpportunityStage? stage, [FromQuery] OpportunityStatus? status, [FromQuery] string? assignedTo, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (opps, total) = await _opportunityService.GetOpportunitiesAsync(searchTerm, stage, status, assignedTo, page, pageSize);

        var dtos = opps.Select(o => new OpportunityReadDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName ?? string.Empty,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage.ToString(),
            Probability = o.Probability,
            WeightedAmount = o.WeightedAmount,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status.ToString(),
            Source = o.Source,
            Notes = o.Notes,
            AssignedTo = o.AssignedTo,
            AssignedUserName = o.AssignedUser?.FullName,
            CreatedDate = o.CreatedDate
        }).ToList();

        return Ok(new PagedResultDto<OpportunityReadDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OpportunityReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var o = await _opportunityService.GetByIdAsync(id, includeRelations: true);
        if (o == null)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = $"Opportunity with ID {id} not found or access denied." });
        }

        return Ok(new OpportunityReadDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName ?? string.Empty,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage.ToString(),
            Probability = o.Probability,
            WeightedAmount = o.WeightedAmount,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status.ToString(),
            Source = o.Source,
            Notes = o.Notes,
            AssignedTo = o.AssignedTo,
            AssignedUserName = o.AssignedUser?.FullName,
            CreatedDate = o.CreatedDate
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(OpportunityReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] OpportunityCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Source = dto.Source?.Trim(),
            Notes = dto.Notes?.Trim(),
            AssignedTo = dto.AssignedTo
        };

        var (succeeded, error, created) = await _opportunityService.CreateOpportunityAsync(opp);
        if (!succeeded)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error });
        }

        var resultDto = new OpportunityReadDto
        {
            OpportunityId = created!.OpportunityId,
            OpportunityName = created.OpportunityName,
            CustomerId = created.CustomerId,
            CustomerName = created.Customer?.CustomerName ?? string.Empty,
            LeadId = created.LeadId,
            Amount = created.Amount,
            Stage = created.Stage.ToString(),
            Probability = created.Probability,
            WeightedAmount = created.WeightedAmount,
            ExpectedCloseDate = created.ExpectedCloseDate,
            Status = created.Status.ToString(),
            Source = created.Source,
            Notes = created.Notes,
            AssignedTo = created.AssignedTo,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetById), new { id = resultDto.OpportunityId }, resultDto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OpportunityReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] OpportunityUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var opp = new Opportunity
        {
            OpportunityId = id,
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Source = dto.Source?.Trim(),
            Notes = dto.Notes?.Trim(),
            AssignedTo = dto.AssignedTo
        };

        var (succeeded, error, updated) = await _opportunityService.UpdateOpportunityAsync(opp);
        if (!succeeded)
        {
            if (error != null && (error.Contains("not found", StringComparison.OrdinalIgnoreCase) || error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = error });
            }
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error });
        }

        return Ok(new OpportunityReadDto
        {
            OpportunityId = updated!.OpportunityId,
            OpportunityName = updated.OpportunityName,
            CustomerId = updated.CustomerId,
            CustomerName = updated.Customer?.CustomerName ?? string.Empty,
            LeadId = updated.LeadId,
            Amount = updated.Amount,
            Stage = updated.Stage.ToString(),
            Probability = updated.Probability,
            WeightedAmount = updated.WeightedAmount,
            ExpectedCloseDate = updated.ExpectedCloseDate,
            Status = updated.Status.ToString(),
            Source = updated.Source,
            Notes = updated.Notes,
            AssignedTo = updated.AssignedTo,
            CreatedDate = updated.CreatedDate
        });
    }
}
