using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities;

public class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(150)]
    public string? UserName { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [Required]
    [MaxLength(50)]
    public string Result { get; set; } = "Success";

    [MaxLength(2000)]
    public string? Details { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }
}
