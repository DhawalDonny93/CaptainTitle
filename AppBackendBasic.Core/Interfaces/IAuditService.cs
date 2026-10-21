namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for audit logging
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an audit entry
    /// </summary>
    Task LogAsync(AuditLogEntry entry);
    
    /// <summary>
    /// Get audit trail for a user
    /// </summary>
    Task<List<AuditLogEntry>> GetUserAuditTrailAsync(int userId, int limit = 100);
    
    /// <summary>
    /// Get audit trail for an entity
    /// </summary>
    Task<List<AuditLogEntry>> GetEntityAuditTrailAsync(string entityType, int entityId, int limit = 100);
    
    /// <summary>
    /// Search audit logs
    /// </summary>
    Task<List<AuditLogEntry>> SearchAuditLogsAsync(AuditSearchCriteria criteria);
}

public class AuditLogEntry
{
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public object? Changes { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? DeviceId { get; set; }
    public string? AdditionalInfo { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class AuditSearchCriteria
{
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? Success { get; set; }
    public int Limit { get; set; } = 100;
}
