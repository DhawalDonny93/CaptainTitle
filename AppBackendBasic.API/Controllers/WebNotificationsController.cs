using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackendBasic.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WebNotificationsController : ControllerBase
{
    private readonly IWebNotificationService _notificationService;
    private readonly ILogger<WebNotificationsController> _logger;

    public WebNotificationsController(
        IWebNotificationService notificationService,
        ILogger<WebNotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get notifications for current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<WebNotificationDto>>> GetNotifications([FromQuery] WebNotificationFilterDto filter)
    {
        try
        {
            var userId = GetUserId();
            filter.UserId = userId;
            
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, filter);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, new { error = "Failed to retrieve notifications" });
        }
    }

    /// <summary>
    /// Get global notifications (visible to all users)
    /// </summary>
    [HttpGet("global")]
    [AllowAnonymous]
    public async Task<ActionResult<List<WebNotificationDto>>> GetGlobalNotifications()
    {
        try
        {
            var notifications = await _notificationService.GetGlobalNotificationsAsync();
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting global notifications");
            return StatusCode(500, new { error = "Failed to retrieve global notifications" });
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, new { error = "Failed to get unread count" });
        }
    }

    /// <summary>
    /// Create a new notification (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WebNotificationDto>> CreateNotification([FromBody] CreateWebNotificationDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var notification = await _notificationService.CreateNotificationAsync(dto);
            return CreatedAtAction(nameof(GetNotifications), new { id = notification.Id }, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, new { error = "Failed to create notification" });
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);
            
            if (!success)
                return NotFound(new { error = "Notification not found" });

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { error = "Failed to mark notification as read" });
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { error = "Failed to mark all notifications as read" });
        }
    }

    /// <summary>
    /// Delete a notification (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteNotification(int id)
    {
        try
        {
            var success = await _notificationService.DeleteNotificationAsync(id);
            
            if (!success)
                return NotFound(new { error = "Notification not found" });

            return Ok(new { message = "Notification deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return StatusCode(500, new { error = "Failed to delete notification" });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
