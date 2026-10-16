using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Security;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string action, string module, string? recordId, object? oldValue, object? newValue, string result = "Success", string? details = null);
    Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 20);
    Task<AuditLog?> GetByIdAsync(long auditLogId);
    Task<byte[]> ExportCsvAsync(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public AuditService(ApplicationDbContext context, ICurrentUserService currentUserService, ILogger<AuditService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task LogAsync(string action, string module, string? recordId, object? oldValue, object? newValue, string result = "Success", string? details = null)
    {
        try
        {
            string? oldJson = oldValue != null ? (oldValue is string s ? s : JsonSerializer.Serialize(oldValue, JsonOptions)) : null;
            string? newJson = newValue != null ? (newValue is string ns ? ns : JsonSerializer.Serialize(newValue, JsonOptions)) : null;

            var audit = new AuditLog
            {
                UserId = _currentUserService.UserId,
                UserName = _currentUserService.UserName ?? _currentUserService.Email ?? "System",
                Action = action,
                Module = module,
                RecordId = recordId,
                OldValue = oldJson,
                NewValue = newJson,
                Result = result,
                Details = details,
                CreatedDate = DateTime.UtcNow,
                IpAddress = _currentUserService.IpAddress
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for {Action} on {Module}", action, module);
        }
    }

    public async Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 20)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            var u = userFilter.Trim().ToLower();
            query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(u)) || (a.UserId != null && a.UserId.Contains(u)));
        }

        if (!string.IsNullOrWhiteSpace(moduleFilter))
        {
            query = query.Where(a => a.Module == moduleFilter);
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action == actionFilter);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedDate < end);
        }

        var total = await query.CountAsync();
        var logs = await query.OrderByDescending(a => a.CreatedDate)
                              .Skip((page - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync();

        return (logs, total);
    }

    public async Task<AuditLog?> GetByIdAsync(long auditLogId)
    {
        return await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.AuditLogId == auditLogId);
    }

    public async Task<byte[]> ExportCsvAsync(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate)
    {
        var (logs, _) = await GetAuditLogsAsync(userFilter, moduleFilter, actionFilter, startDate, endDate, 1, 10000);

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8))
        {
            writer.WriteLine("AuditLogId,CreatedDateUtc,UserName,Action,Module,RecordId,Result,IpAddress,Details");
            foreach (var log in logs)
            {
                var cleanDetails = log.Details?.Replace("\"", "\"\"") ?? "";
                writer.WriteLine($"{log.AuditLogId},\"{log.CreatedDate:yyyy-MM-dd HH:mm:ss}\",\"{log.UserName}\",\"{log.Action}\",\"{log.Module}\",\"{log.RecordId}\",\"{log.Result}\",\"{log.IpAddress}\",\"{cleanDetails}\"");
            }
        }

        return memoryStream.ToArray();
    }
}
