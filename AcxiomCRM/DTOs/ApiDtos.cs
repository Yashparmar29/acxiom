using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Validation;

namespace AcxiomCRM.DTOs;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CustomerReadDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CustomerCreateDto
{
    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [IndianPhone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string? AssignedTo { get; set; }
}

public class CustomerUpdateDto
{
    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [IndianPhone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string? AssignedTo { get; set; }
}

public class LeadReadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public string? Notes { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LeadCreateDto
{
    [Required]
    [MaxLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [IndianPhone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Website;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 100000000)]
    public decimal ExpectedValue { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string? AssignedTo { get; set; }
}

public class LeadUpdateDto
{
    [Required]
    [MaxLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [IndianPhone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; }

    public LeadPriority Priority { get; set; }

    [Range(0, 100000000)]
    public decimal ExpectedValue { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string? AssignedTo { get; set; }
}

public class OpportunityReadDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedAmount { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class OpportunityCreateDto
{
    [Required]
    [MaxLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required]
    [AmountGreaterThanZero]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Required]
    [ValidProbability]
    public int Probability { get; set; } = 10;

    [Required]
    [CloseDateNotInPast]
    public DateTime ExpectedCloseDate { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string? AssignedTo { get; set; }
}

public class OpportunityUpdateDto
{
    [Required]
    [MaxLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required]
    [AmountGreaterThanZero]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; }

    [Required]
    [ValidProbability]
    public int Probability { get; set; }

    [Required]
    [CloseDateNotInPast]
    public DateTime ExpectedCloseDate { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string? AssignedTo { get; set; }
}

public class FollowUpReadDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FollowUpType { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public class FollowUpCreateDto
{
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public int? OpportunityId { get; set; }

    [Required]
    [FutureOrToday]
    public DateTime FollowUpDate { get; set; }

    [Required]
    [MaxLength(150)]
    public string Subject { get; set; } = string.Empty;

    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    public string? AssignedTo { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
