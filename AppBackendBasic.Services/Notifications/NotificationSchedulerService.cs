using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AppBackendBasic.Services.Notifications;

public class NotificationSchedulerService : INotificationSchedulerService
{
    private readonly ApplicationDbContext _context;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<NotificationSchedulerService> _logger;

    public NotificationSchedulerService(
        ApplicationDbContext context,
        IPushNotificationService pushNotificationService,
        ILogger<NotificationSchedulerService> logger)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    public async Task<ScheduledNotification> ScheduleNotificationAsync(
        int userId,
        string title,
        string body,
        DateTime scheduledFor,
        string? sound = "default",
        string? badge = null,
        string? category = null,
        Dictionary<string, string>? data = null)
    {
        try
        {
            var notification = new ScheduledNotification
            {
                UserId = userId,
                Title = title,
                Body = body,
                ScheduledFor = scheduledFor,
                Sound = sound,
                Badge = badge,
                Category = category,
                DataJson = data != null ? JsonSerializer.Serialize(data) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<ScheduledNotification>().Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Scheduled notification {NotificationId} for user {UserId} at {ScheduledFor}",
                notification.Id, userId, scheduledFor);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ScheduledNotification> ScheduleNotificationAsync(
        int userId,
        PushNotificationMessage message,
        DateTime scheduledFor)
    {
        return await ScheduleNotificationAsync(
            userId,
            message.Title,
            message.Body,
            scheduledFor,
            message.Sound,
            message.Badge,
            message.Category,
            message.Data);
    }

    public async Task<bool> CancelScheduledNotificationAsync(int notificationId)
    {
        try
        {
            var notification = await _context.Set<ScheduledNotification>()
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
                return false;

            if (notification.IsSent)
            {
                _logger.LogWarning(
                    "Cannot cancel notification {NotificationId} - already sent",
                    notificationId);
                return false;
            }

            notification.IsCancelled = true;
            notification.CancelledAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled scheduled notification {NotificationId}", notificationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling notification {NotificationId}", notificationId);
            return false;
        }
    }

    public async Task<List<ScheduledNotification>> GetUserScheduledNotificationsAsync(
        int userId,
        bool includeCompleted = false)
    {
        try
        {
            var query = _context.Set<ScheduledNotification>()
                .Where(n => n.UserId == userId);

            if (!includeCompleted)
            {
                query = query.Where(n => !n.IsSent && !n.IsCancelled);
            }

            return await query
                .OrderBy(n => n.ScheduledFor)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduled notifications for user {UserId}", userId);
            return new List<ScheduledNotification>();
        }
    }

    public async Task<ScheduledNotification?> GetScheduledNotificationAsync(int notificationId)
    {
        return await _context.Set<ScheduledNotification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId);
    }

    public async Task ProcessScheduledNotificationsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            // Get all notifications that are due
            var dueNotifications = await _context.Set<ScheduledNotification>()
                .Where(n => !n.IsSent && 
                           !n.IsCancelled && 
                           n.ScheduledFor <= now &&
                           n.RetryCount < n.MaxRetries)
                .OrderBy(n => n.ScheduledFor)
                .Take(100) // Process max 100 at a time
                .ToListAsync();

            if (!dueNotifications.Any())
            {
                _logger.LogDebug("No scheduled notifications due for processing");
                return;
            }

            _logger.LogInformation(
                "Processing {Count} scheduled notifications",
                dueNotifications.Count);

            foreach (var notification in dueNotifications)
            {
                await ProcessNotificationAsync(notification);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled notifications");
        }
    }

    private async Task ProcessNotificationAsync(ScheduledNotification notification)
    {
        try
        {
            // Deserialize data if present
            Dictionary<string, string>? data = null;
            if (!string.IsNullOrEmpty(notification.DataJson))
            {
                data = JsonSerializer.Deserialize<Dictionary<string, string>>(notification.DataJson);
            }

            // Create push notification message
            var message = new PushNotificationMessage
            {
                Title = notification.Title,
                Body = notification.Body,
                Sound = notification.Sound,
                Badge = notification.Badge,
                Category = notification.Category,
                Data = data
            };

            // Send notification
            var result = await _pushNotificationService.SendNotificationAsync(
                notification.UserId,
                message);

            if (result.Success && result.SuccessCount > 0)
            {
                // Mark as sent
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Successfully sent scheduled notification {NotificationId} to user {UserId}",
                    notification.Id, notification.UserId);
            }
            else
            {
                // Increment retry count
                notification.RetryCount++;
                notification.ErrorMessage = $"Failed to send. Attempt {notification.RetryCount}/{notification.MaxRetries}";
                notification.UpdatedAt = DateTime.UtcNow;

                if (notification.RetryCount >= notification.MaxRetries)
                {
                    _logger.LogWarning(
                        "Scheduled notification {NotificationId} failed after {RetryCount} attempts",
                        notification.Id, notification.RetryCount);
                }
                else
                {
                    _logger.LogWarning(
                        "Scheduled notification {NotificationId} failed, will retry. Attempt {RetryCount}/{MaxRetries}",
                        notification.Id, notification.RetryCount, notification.MaxRetries);
                }
            }
        }
        catch (Exception ex)
        {
            notification.RetryCount++;
            notification.ErrorMessage = ex.Message;
            notification.UpdatedAt = DateTime.UtcNow;

            _logger.LogError(ex,
                "Error processing scheduled notification {NotificationId}",
                notification.Id);
        }
    }
}
