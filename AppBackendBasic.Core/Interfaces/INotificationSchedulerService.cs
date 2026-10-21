using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for scheduling notifications for future delivery
/// </summary>
public interface INotificationSchedulerService
{
    /// <summary>
    /// Schedule a notification for future delivery
    /// </summary>
    Task<ScheduledNotification> ScheduleNotificationAsync(
        int userId, 
        string title, 
        string body, 
        DateTime scheduledFor,
        string? sound = "default",
        string? badge = null,
        string? category = null,
        Dictionary<string, string>? data = null);
    
    /// <summary>
    /// Schedule a notification using a message object
    /// </summary>
    Task<ScheduledNotification> ScheduleNotificationAsync(
        int userId,
        PushNotificationMessage message,
        DateTime scheduledFor);
    
    /// <summary>
    /// Cancel a scheduled notification
    /// </summary>
    Task<bool> CancelScheduledNotificationAsync(int notificationId);
    
    /// <summary>
    /// Get all scheduled notifications for a user
    /// </summary>
    Task<List<ScheduledNotification>> GetUserScheduledNotificationsAsync(int userId, bool includeCompleted = false);
    
    /// <summary>
    /// Process all due scheduled notifications
    /// </summary>
    Task ProcessScheduledNotificationsAsync();
    
    /// <summary>
    /// Get notification by ID
    /// </summary>
    Task<ScheduledNotification?> GetScheduledNotificationAsync(int notificationId);
}
