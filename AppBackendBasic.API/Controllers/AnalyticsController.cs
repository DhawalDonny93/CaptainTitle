using System.Security.Claims;
using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Track a server-side event
    /// POST /analytics/events
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventRequestDto request)
    {
        try
        {
            // Validate user ID matches authenticated user
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user authentication" });
            }

            // Override userId with authenticated user's ID for security
            request.UserId = userId.ToString();

            var result = await _analyticsService.TrackEventAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking event");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Set or update user properties
    /// POST /analytics/user-properties
    /// </summary>
    [HttpPost("user-properties")]
    public async Task<IActionResult> SetUserProperties([FromBody] UserPropertiesRequestDto request)
    {
        try
        {
            // Validate user ID matches authenticated user
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user authentication" });
            }

            // Override userId with authenticated user's ID for security
            request.UserId = userId.ToString();

            var result = await _analyticsService.SetUserPropertiesAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting user properties");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Increment a numeric user property
    /// POST /analytics/user-properties/increment
    /// </summary>
    [HttpPost("user-properties/increment")]
    public async Task<IActionResult> IncrementUserProperty([FromBody] IncrementPropertyRequestDto request)
    {
        try
        {
            // Validate user ID matches authenticated user
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user authentication" });
            }

            // Override userId with authenticated user's ID for security
            request.UserId = userId.ToString();

            var result = await _analyticsService.IncrementUserPropertyAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing user property");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Track revenue event
    /// POST /analytics/revenue
    /// </summary>
    [HttpPost("revenue")]
    public async Task<IActionResult> TrackRevenue([FromBody] TrackRevenueRequestDto request)
    {
        try
        {
            // Validate user ID matches authenticated user
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user authentication" });
            }

            // Override userId with authenticated user's ID for security
            request.UserId = userId.ToString();

            var result = await _analyticsService.TrackRevenueAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking revenue");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get analytics data for a specific user
    /// GET /analytics/users/{userId}
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserAnalytics(
        string userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Validate user can only access their own data (unless admin)
            if (!TryGetUserId(out var authenticatedUserId))
            {
                return Unauthorized(new { success = false, message = "Invalid user authentication" });
            }

            // Check if user is accessing their own data or is admin
            if (authenticatedUserId.ToString() != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var result = await _analyticsService.GetUserAnalyticsAsync(userId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user analytics for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get dashboard analytics data (Admin only)
    /// GET /analytics/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDashboardAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? metrics = null)
    {
        try
        {
            var result = await _analyticsService.GetDashboardAnalyticsAsync(startDate, endDate, metrics);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard analytics");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a conversion funnel
    /// POST /analytics/funnels
    /// </summary>
    [HttpPost("funnels")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFunnel([FromBody] CreateFunnelRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Steps == null || request.Steps.Count == 0)
            {
                return BadRequest(new { success = false, message = "Invalid funnel configuration" });
            }

            var result = await _analyticsService.CreateFunnelAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating funnel");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get funnel conversion results
    /// GET /analytics/funnels/{funnelId}
    /// </summary>
    [HttpGet("funnels/{funnelId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFunnelResults(
        string funnelId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _analyticsService.GetFunnelResultsAsync(funnelId, startDate, endDate);
            
            if (result == null)
            {
                return NotFound(new { success = false, message = "Funnel not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting funnel results for funnel {FunnelId}", funnelId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Export analytics data
    /// POST /analytics/export
    /// </summary>
    [HttpPost("export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateExport([FromBody] ExportRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Format))
            {
                return BadRequest(new { success = false, message = "Invalid export configuration" });
            }

            var result = await _analyticsService.CreateExportAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating export");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get export status
    /// GET /analytics/export/{exportId}
    /// </summary>
    [HttpGet("export/{exportId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetExportStatus(string exportId)
    {
        try
        {
            var result = await _analyticsService.GetExportStatusAsync(exportId);
            
            if (result == null)
            {
                return NotFound(new { success = false, message = "Export not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export status for export {ExportId}", exportId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a user cohort
    /// POST /analytics/cohorts
    /// </summary>
    [HttpPost("cohorts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCohort([FromBody] CreateCohortRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Criteria == null)
            {
                return BadRequest(new { success = false, message = "Invalid cohort configuration" });
            }

            var result = await _analyticsService.CreateCohortAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cohort");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
    }
}
