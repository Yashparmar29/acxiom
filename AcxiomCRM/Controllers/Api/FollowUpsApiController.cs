using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<FollowUpReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] FollowUpStatus? status,
        [FromQuery] FollowUpType? type,
        [FromQuery] string? filterType = "all",
        [FromQuery] string? assignedTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (followUps, total) = await _followUpService.GetFollowUpsAsync(searchTerm, status, type, filterType, assignedTo, page, pageSize);

        var dtos = followUps.Select(f => new FollowUpReadDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity?.OpportunityName,
            FollowUpDate = f.FollowUpDate,
            Subject = f.Subject,
            FollowUpType = f.FollowUpType.ToString(),
            Remarks = f.Remarks,
            Status = f.Status.ToString(),
            AssignedTo = f.AssignedTo,
            AssignedUserName = f.AssignedUser?.FullName,
            CompletedDate = f.CompletedDate
        }).ToList();

        return Ok(new PagedResultDto<FollowUpReadDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(FollowUpReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] FollowUpCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            Subject = dto.Subject.Trim(),
            FollowUpType = dto.FollowUpType,
            Remarks = dto.Remarks?.Trim(),
            AssignedTo = dto.AssignedTo
        };

        var (succeeded, error, created) = await _followUpService.CreateFollowUpAsync(followUp);
        if (!succeeded)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error });
        }

        var resultDto = new FollowUpReadDto
        {
            FollowUpId = created!.FollowUpId,
            CustomerId = created.CustomerId,
            LeadId = created.LeadId,
            OpportunityId = created.OpportunityId,
            FollowUpDate = created.FollowUpDate,
            Subject = created.Subject,
            FollowUpType = created.FollowUpType.ToString(),
            Remarks = created.Remarks,
            Status = created.Status.ToString(),
            AssignedTo = created.AssignedTo
        };

        return CreatedAtAction(nameof(GetAll), new { id = resultDto.FollowUpId }, resultDto);
    }
}
