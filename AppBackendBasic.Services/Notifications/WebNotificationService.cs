using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Notifications;

public interface IWebNotificationService
{
    Task<WebNotificationDto> CreateNotificationAsync(CreateWebNotificationDto dto);
    Task<List<WebNotificationDto>> GetUserNotificationsAsync(int userId, WebNotificationFilterDto filter);
    Task<List<WebNotificationDto>> GetGlobalNotificationsAsync();
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> DeleteNotificationAsync(int notificationId);
}

public class WebNotificationService : IWebNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebNotificationService> _logger;

    public WebNotificationService(
        ApplicationDbContext context,
        ILogger<WebNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WebNotificationDto> CreateNotificationAsync(CreateWebNotificationDto dto)
    {
        var notification = new Notification
        {
            Title = dto.Title,
            Body = dto.Body,
            Type = dto.Type,
            Icon = dto.Icon,
            UserId = dto.UserId ?? 0,
            IsGlobal = dto.IsGlobal,
            IsDismissible = dto.IsDismissible,
            DurationMs = dto.DurationMs,
            ActionUrl = dto.ActionUrl,
            ActionText = dto.ActionText,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created web notification {Id} for user {UserId}", 
            notification.Id, notification.UserId);

        return MapToDto(notification);
    }

    public async Task<List<WebNotificationDto>> GetUserNotificationsAsync(
        int userId, 
        WebNotificationFilterDto filter)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId || n.IsGlobal);

        if (!filter.IncludeExpired)
        {
            query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);
        }

        if (filter.Type != null)
        {
            query = query.Where(n => n.Type == filter.Type);
        }

        if (filter.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == filter.IsRead.Value);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<List<WebNotificationDto>> GetGlobalNotificationsAsync()
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.IsGlobal && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var exists = await _context.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.Id == notificationId && 
                          (n.UserId == userId || n.IsGlobal));

        if (!exists)
            return false;

        await _context.Notifications
            .Where(n => n.Id == notificationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        await _context.Notifications
            .Where(n => (n.UserId == userId || n.IsGlobal) && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));

        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => (n.UserId == userId || n.IsGlobal) && 
                           !n.IsRead &&
                           (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var deleted = await _context.Notifications
            .Where(n => n.Id == notificationId)
            .ExecuteDeleteAsync();

        return deleted > 0;
    }

    private static WebNotificationDto MapToDto(Notification notification)
    {
        return new WebNotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Body = notification.Body,
            Type = notification.Type,
            Icon = notification.Icon,
            IsDismissible = notification.IsDismissible,
            DurationMs = notification.DurationMs,
            ActionUrl = notification.ActionUrl,
            ActionText = notification.ActionText,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ExpiresAt = notification.ExpiresAt
        };
    }
}
