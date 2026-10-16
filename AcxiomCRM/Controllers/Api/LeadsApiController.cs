using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<LeadReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, [FromQuery] LeadStatus? status, [FromQuery] string? assignedTo, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (leads, total) = await _leadService.GetLeadsAsync(searchTerm, status, assignedTo, page, pageSize);

        var dtos = leads.Select(l => new LeadReadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source.ToString(),
            Status = l.Status.ToString(),
            Priority = l.Priority.ToString(),
            ExpectedValue = l.ExpectedValue,
            Notes = l.Notes,
            AssignedTo = l.AssignedTo,
            AssignedUserName = l.AssignedUser?.FullName,
            CreatedDate = l.CreatedDate
        }).ToList();

        return Ok(new PagedResultDto<LeadReadDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LeadReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var l = await _leadService.GetByIdAsync(id, includeRelations: false);
        if (l == null)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = $"Lead with ID {id} not found or access denied." });
        }

        return Ok(new LeadReadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source.ToString(),
            Status = l.Status.ToString(),
            Priority = l.Priority.ToString(),
            ExpectedValue = l.ExpectedValue,
            Notes = l.Notes,
            AssignedTo = l.AssignedTo,
            AssignedUserName = l.AssignedUser?.FullName,
            CreatedDate = l.CreatedDate
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(LeadReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] LeadCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var lead = new Lead
        {
            LeadName = dto.LeadName.Trim(),
            Email = dto.Email?.Trim().ToLower(),
            Phone = dto.Phone?.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Source = dto.Source,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Notes = dto.Notes?.Trim(),
            AssignedTo = dto.AssignedTo,
            Status = LeadStatus.New
        };

        var (succeeded, error, created) = await _leadService.CreateLeadAsync(lead);
        if (!succeeded)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error });
        }

        var resultDto = new LeadReadDto
        {
            LeadId = created!.LeadId,
            LeadCode = created.LeadCode,
            LeadName = created.LeadName,
            Email = created.Email,
            Phone = created.Phone,
            CompanyName = created.CompanyName,
            Source = created.Source.ToString(),
            Status = created.Status.ToString(),
            Priority = created.Priority.ToString(),
            ExpectedValue = created.ExpectedValue,
            Notes = created.Notes,
            AssignedTo = created.AssignedTo,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetById), new { id = resultDto.LeadId }, resultDto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(LeadReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] LeadUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var lead = new Lead
        {
            LeadId = id,
            LeadName = dto.LeadName.Trim(),
            Email = dto.Email?.Trim().ToLower(),
            Phone = dto.Phone?.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Source = dto.Source,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Notes = dto.Notes?.Trim(),
            AssignedTo = dto.AssignedTo
        };

        var (succeeded, error, updated) = await _leadService.UpdateLeadAsync(lead);
        if (!succeeded)
        {
            if (error != null && (error.Contains("not found", StringComparison.OrdinalIgnoreCase) || error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = error });
            }
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error });
        }

        return Ok(new LeadReadDto
        {
            LeadId = updated!.LeadId,
            LeadCode = updated.LeadCode,
            LeadName = updated.LeadName,
            Email = updated.Email,
            Phone = updated.Phone,
            CompanyName = updated.CompanyName,
            Source = updated.Source.ToString(),
            Status = updated.Status.ToString(),
            Priority = updated.Priority.ToString(),
            ExpectedValue = updated.ExpectedValue,
            Notes = updated.Notes,
            AssignedTo = updated.AssignedTo,
            CreatedDate = updated.CreatedDate
        });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var (succeeded, error) = await _leadService.DeleteLeadAsync(id);
        if (!succeeded)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = error });
        }

        return NoContent();
    }
}
