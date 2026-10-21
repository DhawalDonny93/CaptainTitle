using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Core.DTOs;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/scheduled-notifications")]
public class ScheduledNotificationsController : ControllerBase
{
    private readonly INotificationSchedulerService _schedulerService;
    private readonly ILogger<ScheduledNotificationsController> _logger;

    public ScheduledNotificationsController(
        INotificationSchedulerService schedulerService,
        ILogger<ScheduledNotificationsController> logger)
    {
        _schedulerService = schedulerService;
        _logger = logger;
    }

    /// <summary>
    /// Schedule a notification for future delivery
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ScheduleNotification([FromBody] ScheduleNotificationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        if (request.ScheduledFor <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Scheduled time must be in the future" });
        }

        try
        {
            // Convert Dictionary<string, object> to Dictionary<string, string>
            var dataDict = request.Data?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty
            );

            var notification = await _schedulerService.ScheduleNotificationAsync(
                userInt,
                request.Title,
                request.Body,
                request.ScheduledFor,
                request.Sound,
                request.Badge,
                request.Category,
                dataDict);

            return Ok(new
            {
                success = true,
                notificationId = notification.Id,
                scheduledFor = notification.ScheduledFor,
                message = "Notification scheduled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to schedule notification" });
        }
    }

    /// <summary>
    /// Get all scheduled notifications for current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetScheduledNotifications([FromQuery] bool includeCompleted = false)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var notifications = await _schedulerService.GetUserScheduledNotificationsAsync(
                userInt,
                includeCompleted);

            return Ok(new
            {
                success = true,
                count = notifications.Count,
                notifications = notifications.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Body,
                    n.ScheduledFor,
                    n.IsSent,
                    n.SentAt,
                    n.IsCancelled,
                    n.CancelledAt,
                    n.RetryCount,
                    n.ErrorMessage,
                    n.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduled notifications for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to retrieve notifications" });
        }
    }

    /// <summary>
    /// Get specific scheduled notification
    /// </summary>
    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetScheduledNotification(int notificationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var notification = await _schedulerService.GetScheduledNotificationAsync(notificationId);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            // Verify ownership
            if (notification.UserId != userInt)
                return Forbid();

            return Ok(new
            {
                success = true,
                notification = new
                {
                    notification.Id,
                    notification.Title,
                    notification.Body,
                    notification.ScheduledFor,
                    notification.IsSent,
                    notification.SentAt,
                    notification.IsCancelled,
                    notification.CancelledAt,
                    notification.RetryCount,
                    notification.ErrorMessage,
                    notification.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId}", notificationId);
            return StatusCode(500, new { message = "Failed to retrieve notification" });
        }
    }

    /// <summary>
    /// Cancel a scheduled notification
    /// </summary>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> CancelScheduledNotification(int notificationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            // Verify ownership
            var notification = await _schedulerService.GetScheduledNotificationAsync(notificationId);
            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            if (notification.UserId != userInt)
                return Forbid();

            var success = await _schedulerService.CancelScheduledNotificationAsync(notificationId);

            if (!success)
                return BadRequest(new { message = "Failed to cancel notification. It may have already been sent." });

            return Ok(new
            {
                success = true,
                message = "Notification cancelled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling notification {NotificationId}", notificationId);
            return StatusCode(500, new { message = "Failed to cancel notification" });
        }
    }
}

// DTOs moved to AppBackendBasic.Core.DTOs namespace
