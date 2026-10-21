using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;

namespace AppBackendBasic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/in-app-notifications")]
public class InAppNotificationsController : ControllerBase
{
    private readonly IInAppNotificationService _notificationService;
    private readonly ILogger<InAppNotificationsController> _logger;

    public InAppNotificationsController(
        IInAppNotificationService notificationService,
        ILogger<InAppNotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get user notifications with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeRead = true,
        [FromQuery] bool includeArchived = false)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var (notifications, totalCount) = await _notificationService.GetUserNotificationsAsync(
                userInt,
                page,
                pageSize,
                includeRead,
                includeArchived);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new
            {
                success = true,
                page,
                pageSize,
                totalCount,
                totalPages,
                notifications = notifications.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Body,
                    n.Type,
                    n.Category,
                    n.ActionUrl,
                    n.ActionText,
                    n.ImageUrl,
                    n.IsRead,
                    n.ReadAt,
                    n.Priority,
                    n.CreatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to retrieve notifications" });
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var count = await _notificationService.GetUnreadCountAsync(userInt);

            return Ok(new
            {
                success = true,
                unreadCount = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to get unread count" });
        }
    }

    /// <summary>
    /// Get specific notification
    /// </summary>
    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetNotification(int notificationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var notification = await _notificationService.GetNotificationAsync(notificationId, userInt);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            return Ok(new
            {
                success = true,
                notification = new
                {
                    notification.Id,
                    notification.Title,
                    notification.Body,
                    notification.Type,
                    notification.Category,
                    notification.ActionUrl,
                    notification.ActionText,
                    notification.ImageUrl,
                    notification.IsRead,
                    notification.ReadAt,
                    notification.Priority,
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
    /// Mark notification as read
    /// </summary>
    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var success = await _notificationService.MarkAsReadAsync(notificationId, userInt);

            if (!success)
                return NotFound(new { message = "Notification not found" });

            return Ok(new
            {
                success = true,
                message = "Notification marked as read"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return StatusCode(500, new { message = "Failed to mark notification as read" });
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var count = await _notificationService.MarkAllAsReadAsync(userInt);

            return Ok(new
            {
                success = true,
                message = $"Marked {count} notifications as read",
                count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to mark all as read" });
        }
    }

    /// <summary>
    /// Archive notification
    /// </summary>
    [HttpPut("{notificationId}/archive")]
    public async Task<IActionResult> ArchiveNotification(int notificationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var success = await _notificationService.ArchiveNotificationAsync(notificationId, userInt);

            if (!success)
                return NotFound(new { message = "Notification not found" });

            return Ok(new
            {
                success = true,
                message = "Notification archived"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving notification {NotificationId}", notificationId);
            return StatusCode(500, new { message = "Failed to archive notification" });
        }
    }

    /// <summary>
    /// Delete notification
    /// </summary>
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> DeleteNotification(int notificationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var success = await _notificationService.DeleteNotificationAsync(notificationId, userInt);

            if (!success)
                return NotFound(new { message = "Notification not found" });

            return Ok(new
            {
                success = true,
                message = "Notification deleted"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            return StatusCode(500, new { message = "Failed to delete notification" });
        }
    }

    /// <summary>
    /// Delete all read notifications
    /// </summary>
    [HttpDelete("read-all")]
    public async Task<IActionResult> DeleteAllRead()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var count = await _notificationService.DeleteAllReadAsync(userInt);

            return Ok(new
            {
                success = true,
                message = $"Deleted {count} read notifications",
                count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting read notifications for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to delete notifications" });
        }
    }

    /// <summary>
    /// Sync notifications on app open - returns unread count and optionally marks delivered as read
    /// </summary>
    [HttpPost("sync-on-open")]
    public async Task<IActionResult> SyncOnAppOpen([FromQuery] bool markDeliveredAsRead = false)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var unreadCount = await _notificationService.GetUnreadCountAsync(userInt);
            
            // Optionally mark all as read when app opens (for badge clearing)
            if (markDeliveredAsRead && unreadCount > 0)
            {
                await _notificationService.MarkAllAsReadAsync(userInt);
                unreadCount = 0;
            }

            return Ok(new
            {
                success = true,
                unreadCount,
                badgeCleared = markDeliveredAsRead,
                message = markDeliveredAsRead ? "All notifications marked as read" : "Notification count retrieved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing notifications on app open for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to sync notifications" });
        }
    }

    /// <summary>
    /// Create a test notification (for testing purposes)
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> CreateTestNotification()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userInt))
        {
            return Unauthorized(new { message = "Invalid user ID in token." });
        }

        try
        {
            var notification = await _notificationService.CreateNotificationAsync(
                userInt,
                "Test Notification",
                "This is a test notification from the in-app notification center.",
                "info",
                "test",
                "/profile",
                "View Profile",
                0);

            return Ok(new
            {
                success = true,
                message = "Test notification created",
                notificationId = notification.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test notification for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to create test notification" });
        }
    }
}
