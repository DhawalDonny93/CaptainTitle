using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;
    private readonly ILogger<PoliciesController> _logger;

    public PoliciesController(IPolicyService policyService, ILogger<PoliciesController> logger)
    {
        _policyService = policyService;
        _logger = logger;
    }

    [HttpGet("privacy")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPrivacyPolicy()
    {
        var policy = await _policyService.GetCurrentPolicyAsync("Privacy");
        
        if (policy == null)
        {
            return NotFound(new { message = "Privacy policy not found." });
        }

        return Ok(new
        {
            id = policy.Id,
            type = policy.Type,
            version = policy.Version,
            content = policy.Content,
            summary = policy.Summary,
            effectiveDate = policy.EffectiveDate
        });
    }

    [HttpGet("terms")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTermsOfService()
    {
        var policy = await _policyService.GetCurrentPolicyAsync("Terms");
        
        if (policy == null)
        {
            return NotFound(new { message = "Terms of service not found." });
        }

        return Ok(new
        {
            id = policy.Id,
            type = policy.Type,
            version = policy.Version,
            content = policy.Content,
            summary = policy.Summary,
            effectiveDate = policy.EffectiveDate
        });
    }

    [HttpPost("accept")]
    [Authorize]
    public async Task<IActionResult> AcceptPolicy([FromBody] AcceptPolicyRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = Request.Headers["User-Agent"].ToString();
        
        var success = await _policyService.AcceptPolicyAsync(
            userInt, 
            request.PolicyId, 
            ipAddress, 
            userAgent, 
            request.DeviceId);

        if (!success)
        {
            return BadRequest(new { message = "Failed to record policy acceptance." });
        }

        _logger.LogInformation("User {UserId} accepted policy {PolicyId}", userId, request.PolicyId);
        
        return Ok(new { success = true, message = "Policy accepted successfully." });
    }

    [HttpGet("check-updates")]
    [Authorize]
    public async Task<IActionResult> CheckPolicyUpdates()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var status = await _policyService.CheckPolicyUpdatesAsync(userInt);

        return Ok(new
        {
            requiresAcceptance = status.RequiresAcceptance,
            pendingPolicies = status.PendingPolicies.Select(p => new
            {
                id = p.Id,
                type = p.Type,
                version = p.Version,
                summary = p.Summary,
                effectiveDate = p.EffectiveDate
            }).ToList()
        });
    }

    [HttpGet("acceptance-history")]
    [Authorize]
    public async Task<IActionResult> GetAcceptanceHistory()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        var history = await _policyService.GetUserAcceptanceHistoryAsync(userInt);

        return Ok(history.Select(a => new
        {
            policyType = a.Policy.Type,
            policyVersion = a.Policy.Version,
            acceptedAt = a.AcceptedAt,
        }).ToList());
    }
}

public class AcceptPolicyRequest
{
    public int PolicyId { get; set; }
    public string? DeviceId { get; set; }
}

public class PolicyDTO
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string Version { get; set; }
    public string Content { get; set; }
    public string Summary { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class AcceptanceHistoryDTO
{
    public string PolicyType { get; set; }
    public string PolicyVersion { get; set; }
    public DateTime AcceptedAt { get; set; }
    public string IpAddress { get; set; }
    public string DeviceId { get; set; }
}
