using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet("my-audit-trail")]
    public async Task<IActionResult> GetMyAuditTrail([FromQuery] int limit = 100)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var auditTrail = await _auditService.GetUserAuditTrailAsync(userInt, limit);

        return Ok(auditTrail.Select(a => new
        {
            action = a.Action,
            entityType = a.EntityType,
            entityId = a.EntityId,
            changes = a.Changes,
            ipAddress = a.IpAddress,
            deviceId = a.DeviceId,
            success = a.Success,
            errorMessage = a.ErrorMessage
        }));
    }

    [HttpPost("search")]
    [Authorize(Roles = "Admin")] // Admin only
    public async Task<IActionResult> SearchAuditLogs([FromBody] AuditSearchRequest request)
    {
        var criteria = new AuditSearchCriteria
        {
            UserId = request.UserId,
            Action = request.Action,
            EntityType = request.EntityType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Success = request.Success,
            Limit = request.Limit ?? 100
        };

        var logs = await _auditService.SearchAuditLogsAsync(criteria);

        return Ok(logs);
    }
}
