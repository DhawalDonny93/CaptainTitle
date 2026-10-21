using System.Text.Json;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Compliance;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = entry.UserId,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Changes = entry.Changes != null ? JsonSerializer.Serialize(entry.Changes) : null,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                DeviceId = entry.DeviceId,
                AdditionalInfo = entry.AdditionalInfo,
                Success = entry.Success,
                ErrorMessage = entry.ErrorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<AuditLog>().Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {Action} on {EntityType} by user {UserId}", 
                entry.Action, entry.EntityType, entry.UserId);
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging should never break the application
            _logger.LogError(ex, "Error creating audit log for action {Action}", entry.Action);
        }
    }

    public async Task<List<AuditLogEntry>> GetUserAuditTrailAsync(int userId, int limit = 100)
    {
        try
        {
            var logs = await _context.Set<AuditLog>()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return logs.Select(MapToEntry).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for user {UserId}", userId);
            return new List<AuditLogEntry>();
        }
    }

    public async Task<List<AuditLogEntry>> GetEntityAuditTrailAsync(string entityType, int entityId, int limit = 100)
    {
        try
        {
            var logs = await _context.Set<AuditLog>()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return logs.Select(MapToEntry).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for {EntityType} {EntityId}", entityType, entityId);
            return new List<AuditLogEntry>();
        }
    }

    public async Task<List<AuditLogEntry>> SearchAuditLogsAsync(AuditSearchCriteria criteria)
    {
        try
        {
            var query = _context.Set<AuditLog>().AsQueryable();

            if (criteria.UserId.HasValue)
                query = query.Where(a => a.UserId == criteria.UserId.Value);

            if (!string.IsNullOrEmpty(criteria.Action))
                query = query.Where(a => a.Action == criteria.Action);

            if (!string.IsNullOrEmpty(criteria.EntityType))
                query = query.Where(a => a.EntityType == criteria.EntityType);

            if (criteria.StartDate.HasValue)
                query = query.Where(a => a.CreatedAt >= criteria.StartDate.Value);

            if (criteria.EndDate.HasValue)
                query = query.Where(a => a.CreatedAt <= criteria.EndDate.Value);

            if (criteria.Success.HasValue)
                query = query.Where(a => a.Success == criteria.Success.Value);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(criteria.Limit)
                .ToListAsync();

            return logs.Select(MapToEntry).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit logs");
            return new List<AuditLogEntry>();
        }
    }

    private AuditLogEntry MapToEntry(AuditLog log)
    {
        object? changes = null;
        if (!string.IsNullOrEmpty(log.Changes))
        {
            try
            {
                changes = JsonSerializer.Deserialize<object>(log.Changes);
            }
            catch
            {
                changes = log.Changes;
            }
        }

        return new AuditLogEntry
        {
            UserId = log.UserId,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Changes = changes,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            DeviceId = log.DeviceId,
            AdditionalInfo = log.AdditionalInfo,
            Success = log.Success,
            ErrorMessage = log.ErrorMessage
        };
    }
}
