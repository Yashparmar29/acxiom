using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Security;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize(AuthenticationSchemes = "Bearer")]
[Produces("application/json")]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IOwnershipScopeHelper _scopeHelper;

    public CustomersApiController(ICustomerService customerService, IOwnershipScopeHelper scopeHelper)
    {
        _customerService = customerService;
        _scopeHelper = scopeHelper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<CustomerReadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, [FromQuery] CustomerStatus? status, [FromQuery] string? assignedTo, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (customers, total) = await _customerService.GetCustomersAsync(searchTerm, status, assignedTo, page, pageSize);

        var dtos = customers.Select(c => new CustomerReadDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status.ToString(),
            Notes = c.Notes,
            AssignedTo = c.AssignedTo,
            AssignedUserName = c.AssignedUser?.FullName,
            CreatedDate = c.CreatedDate
        }).ToList();

        return Ok(new PagedResultDto<CustomerReadDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _customerService.GetByIdAsync(id, includeRelations: false);
        if (c == null)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = $"Customer with ID {id} not found or access denied." });
        }

        return Ok(new CustomerReadDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status.ToString(),
            Notes = c.Notes,
            AssignedTo = c.AssignedTo,
            AssignedUserName = c.AssignedUser?.FullName,
            CreatedDate = c.CreatedDate
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var customer = new Customer
        {
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Notes = dto.Notes?.Trim(),
            AssignedTo = dto.AssignedTo,
            Status = CustomerStatus.Active
        };

        var (succeeded, error, created) = await _customerService.CreateCustomerAsync(customer);
        if (!succeeded)
        {
            if (error != null && error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ProblemDetails { Status = 409, Title = "Conflict", Detail = error });
            }
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error });
        }

        var resultDto = new CustomerReadDto
        {
            CustomerId = created!.CustomerId,
            CustomerCode = created.CustomerCode,
            CustomerName = created.CustomerName,
            Email = created.Email,
            Phone = created.Phone,
            CompanyName = created.CompanyName,
            Address = created.Address,
            City = created.City,
            State = created.State,
            Status = created.Status.ToString(),
            Notes = created.Notes,
            AssignedTo = created.AssignedTo,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetById), new { id = resultDto.CustomerId }, resultDto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CustomerReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var customer = new Customer
        {
            CustomerId = id,
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Status = dto.Status,
            Notes = dto.Notes?.Trim(),
            AssignedTo = dto.AssignedTo
        };

        var (succeeded, error, updated) = await _customerService.UpdateCustomerAsync(customer);
        if (!succeeded)
        {
            if (error != null && (error.Contains("not found", StringComparison.OrdinalIgnoreCase) || error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = error });
            }
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad Request", Detail = error });
        }

        return Ok(new CustomerReadDto
        {
            CustomerId = updated!.CustomerId,
            CustomerCode = updated.CustomerCode,
            CustomerName = updated.CustomerName,
            Email = updated.Email,
            Phone = updated.Phone,
            CompanyName = updated.CompanyName,
            Address = updated.Address,
            City = updated.City,
            State = updated.State,
            Status = updated.Status.ToString(),
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
        var (succeeded, error) = await _customerService.DeactivateCustomerAsync(id);
        if (!succeeded)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not Found", Detail = error });
        }

        return NoContent();
    }
}
