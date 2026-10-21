using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AppBackendBasic.Services.Notifications;

public class InAppNotificationService : IInAppNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InAppNotificationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public InAppNotificationService(
        ApplicationDbContext context,
        ILogger<InAppNotificationService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<InAppNotification> CreateNotificationAsync(
        int userId,
        string title,
        string body,
        string type = "info",
        string? category = null,
        string? actionUrl = null,
        string? actionText = null,
        int priority = 0,
        DateTime? expiresAt = null)
    {
        try
        {
            var notification = new InAppNotification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                Category = category,
                ActionUrl = actionUrl,
                ActionText = actionText,
                Priority = priority,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<InAppNotification>().Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created in-app notification {NotificationId} for user {UserId}",
                notification.Id, userId);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating in-app notification for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(List<InAppNotification> Notifications, int TotalCount)> GetUserNotificationsAsync(
        int userId,
        int page = 1,
        int pageSize = 20,
        bool includeRead = true,
        bool includeArchived = false)
    {
        try
        {
            var query = _context.Set<InAppNotification>()
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            // Filter out expired notifications
            query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            if (!includeArchived)
            {
                query = query.Where(n => !n.IsArchived);
            }

            var totalCount = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            return (new List<InAppNotification>(), 0);
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        try
        {
            return await _context.Set<InAppNotification>()
                .AsNoTracking()
                .Where(n => n.UserId == userId && 
                           !n.IsRead && 
                           !n.IsArchived &&
                           (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow))
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        try
        {
            var exists = await _context.Set<InAppNotification>()
                .AsNoTracking()
                .AnyAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsRead);

            if (!exists)
                return false;

            await _context.Set<InAppNotification>()
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow)
                    .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

            _logger.LogInformation(
                "Marked notification {NotificationId} as read for user {UserId}",
                notificationId, userId);

            // Update badge count via silent push notification
            await UpdateBadgeCountAsync(userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error marking notification {NotificationId} as read for user {UserId}",
                notificationId, userId);
            return false;
        }
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        try
        {
            var count = await _context.Set<InAppNotification>()
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            if (count > 0)
            {
                await _context.Set<InAppNotification>()
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(n => n.IsRead, true)
                        .SetProperty(n => n.ReadAt, DateTime.UtcNow)
                        .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));
            }

            _logger.LogInformation(
                "Marked {Count} notifications as read for user {UserId}",
                count, userId);

            // Update badge count via silent push notification
            if (count > 0)
            {
                await UpdateBadgeCountAsync(userId);
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> ArchiveNotificationAsync(int notificationId, int userId)
    {
        try
        {
            var exists = await _context.Set<InAppNotification>()
                .AsNoTracking()
                .AnyAsync(n => n.Id == notificationId && n.UserId == userId);

            if (!exists)
                return false;

            await _context.Set<InAppNotification>()
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.IsArchived, true)
                    .SetProperty(n => n.ArchivedAt, DateTime.UtcNow)
                    .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

            _logger.LogInformation(
                "Archived notification {NotificationId} for user {UserId}",
                notificationId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error archiving notification {NotificationId} for user {UserId}",
                notificationId, userId);
            return false;
        }
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
    {
        try
        {
            var deleted = await _context.Set<InAppNotification>()
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .ExecuteDeleteAsync();

            if (deleted == 0)
                return false;

            _logger.LogInformation(
                "Deleted notification {NotificationId} for user {UserId}",
                notificationId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting notification {NotificationId} for user {UserId}",
                notificationId, userId);
            return false;
        }
    }

    public async Task<int> DeleteAllReadAsync(int userId)
    {
        try
        {
            var count = await _context.Set<InAppNotification>()
                .Where(n => n.UserId == userId && n.IsRead)
                .ExecuteDeleteAsync();

            _logger.LogInformation(
                "Deleted {Count} read notifications for user {UserId}",
                count, userId);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting read notifications for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<InAppNotification?> GetNotificationAsync(int notificationId, int userId)
    {
        try
        {
            return await _context.Set<InAppNotification>()
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting notification {NotificationId} for user {UserId}",
                notificationId, userId);
            return null;
        }
    }

    private async Task UpdateBadgeCountAsync(int userId)
    {
        try
        {
            var unreadCount = await GetUnreadCountAsync(userId);
            
            // Send silent push notification to update badge
            var message = new PushNotificationMessage
            {
                Title = "",
                Body = "",
                Badge = unreadCount.ToString(),
                ContentAvailable = true // Silent notification
            };

            // Resolve IPushNotificationService to avoid circular dependency
            using var scope = _serviceProvider.CreateScope();
            var pushNotificationService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
            await pushNotificationService.SendNotificationAsync(userId, message);
            
            _logger.LogDebug("Updated badge count to {Count} for user {UserId}", unreadCount, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating badge count for user {UserId}", userId);
        }
    }
}
