using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Services.Notifications;

namespace AppBackendBasic.API.Helpers;

/// <summary>
/// Helper class for creating notifications throughout the application
/// </summary>
public static class NotificationHelper
{
    /// <summary>
    /// Create a success notification
    /// </summary>
    public static async Task NotifySuccess(
        IWebNotificationService service,
        int userId,
        string message,
        string? title = null,
        string? actionUrl = null,
        string? actionText = null)
    {
        await service.CreateNotificationAsync(new CreateWebNotificationDto
        {
            Type = "success",
            Title = title ?? "Success",
            Body = message,
            UserId = userId,
            IsGlobal = false,
            DurationMs = 5000,
            ActionUrl = actionUrl,
            ActionText = actionText
        });
    }

    /// <summary>
    /// Create an error notification
    /// </summary>
    public static async Task NotifyError(
        IWebNotificationService service,
        int userId,
        string message,
        string? title = null)
    {
        await service.CreateNotificationAsync(new CreateWebNotificationDto
        {
            Type = "error",
            Title = title ?? "Error",
            Body = message,
            UserId = userId,
            IsGlobal = false,
            DurationMs = 8000
        });
    }

    /// <summary>
    /// Create a warning notification
    /// </summary>
    public static async Task NotifyWarning(
        IWebNotificationService service,
        int userId,
        string message,
        string? title = null)
    {
        await service.CreateNotificationAsync(new CreateWebNotificationDto
        {
            Type = "warning",
            Title = title ?? "Warning",
            Body = message,
            UserId = userId,
            IsGlobal = false,
            DurationMs = 6000
        });
    }

    /// <summary>
    /// Create an info notification
    /// </summary>
    public static async Task NotifyInfo(
        IWebNotificationService service,
        int userId,
        string message,
        string? title = null)
    {
        await service.CreateNotificationAsync(new CreateWebNotificationDto
        {
            Type = "info",
            Title = title ?? "Info",
            Body = message,
            UserId = userId,
            IsGlobal = false,
            DurationMs = 5000
        });
    }

    /// <summary>
    /// Create a global notification (visible to all users)
    /// </summary>
    public static async Task NotifyGlobal(
        IWebNotificationService service,
        string type,
        string message,
        string? title = null,
        DateTime? expiresAt = null)
    {
        await service.CreateNotificationAsync(new CreateWebNotificationDto
        {
            Type = type,
            Title = title ?? type.ToUpper(),
            Body = message,
            IsGlobal = true,
            DurationMs = 10000,
            ExpiresAt = expiresAt
        });
    }
}
