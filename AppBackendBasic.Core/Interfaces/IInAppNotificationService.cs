using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for managing in-app notifications
/// </summary>
public interface IInAppNotificationService
{
    /// <summary>
    /// Create a new in-app notification
    /// </summary>
    Task<InAppNotification> CreateNotificationAsync(
        int userId, 
        string title, 
        string body,
        string type = "info",
        string? category = null,
        string? actionUrl = null,
        string? actionText = null,
        int priority = 0,
        DateTime? expiresAt = null);
    
    /// <summary>
    /// Get user notifications with pagination
    /// </summary>
    Task<(List<InAppNotification> Notifications, int TotalCount)> GetUserNotificationsAsync(
        int userId, 
        int page = 1, 
        int pageSize = 20,
        bool includeRead = true,
        bool includeArchived = false);
    
    /// <summary>
    /// Get unread notification count
    /// </summary>
    Task<int> GetUnreadCountAsync(int userId);
    
    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    
    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    Task<int> MarkAllAsReadAsync(int userId);
    
    /// <summary>
    /// Archive notification
    /// </summary>
    Task<bool> ArchiveNotificationAsync(int notificationId, int userId);
    
    /// <summary>
    /// Delete notification
    /// </summary>
    Task<bool> DeleteNotificationAsync(int notificationId, int userId);
    
    /// <summary>
    /// Delete all read notifications
    /// </summary>
    Task<int> DeleteAllReadAsync(int userId);
    
    /// <summary>
    /// Get notification by ID
    /// </summary>
    Task<InAppNotification?> GetNotificationAsync(int notificationId, int userId);
}
