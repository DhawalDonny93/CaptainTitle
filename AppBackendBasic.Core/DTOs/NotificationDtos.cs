namespace AppBackendBasic.Core.DTOs;

/// <summary>
/// Request to register a device for push notifications
/// </summary>
public class RegisterDeviceRequest
{
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "iOS";
    public string? DeviceId { get; set; }
    public string? DeviceModel { get; set; }
    public string? AppVersion { get; set; }
}

/// <summary>
/// Request to unregister a device from push notifications
/// </summary>
public class UnregisterDeviceRequest
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Request to send notification to a specific user
/// </summary>
public class SendNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public int? Priority { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Request to broadcast notification to all users
/// </summary>
public class BroadcastNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public int? Priority { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Request to send notification to a user segment
/// </summary>
public class SendToSegmentRequest
{
    public string Segment { get; set; } = string.Empty; // verified, unverified, subscribers, free, new
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public int? Priority { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Request to schedule a notification
/// </summary>
public class ScheduleNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Category { get; set; }
    public DateTime ScheduledFor { get; set; }
    public string? TargetSegment { get; set; }
    public List<Guid>? TargetUserIds { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public int? Priority { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public string? Sound { get; set; } = "default";
    public string? Badge { get; set; }
}

/// <summary>
/// Response for notification operations
/// </summary>
public class NotificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? RecipientCount { get; set; }
    public NotificationResults? Results { get; set; }
}

/// <summary>
/// Detailed notification results
/// </summary>
public class NotificationResults
{
    public int TotalUsers { get; set; }
    public int InAppNotificationsCreated { get; set; }
    public int PushNotificationsSent { get; set; }
    public int PushNotificationsFailed { get; set; }
    public string? Segment { get; set; }
}

/// <summary>
/// Notification history item
/// </summary>
public class NotificationHistoryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty; // Alias for Body for backward compatibility
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int Recipients { get; set; }
    public DateTime SentAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Alias for NotificationHistoryDto for backward compatibility
/// </summary>
public class NotificationHistory : NotificationHistoryDto { }

/// <summary>
/// Notification history response with pagination
/// </summary>
public class NotificationHistoryResponse
{
    public bool Success { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<NotificationHistoryDto> Notifications { get; set; } = new();
}

/// <summary>
/// Notification statistics
/// </summary>
public class NotificationStatsDto
{
    public int TotalSent { get; set; }
    public int SentToday { get; set; }
    public int SentThisWeek { get; set; }
    public int SentThisMonth { get; set; }
    public double DeliveryRate { get; set; }
    public double OpenRate { get; set; }
}

/// <summary>
/// Alias for NotificationStatsDto for backward compatibility
/// </summary>
public class NotificationStats : NotificationStatsDto { }

/// <summary>
/// Request to update notification preferences
/// </summary>
public class UpdateNotificationPreferencesRequest
{
    public bool? PushEnabled { get; set; }
    public bool? EmailEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public bool? InAppEnabled { get; set; }
    public bool? MarketingEnabled { get; set; }
    public bool? UpdatesEnabled { get; set; }
    public bool? RemindersEnabled { get; set; }
    public CategoryPreferences? Categories { get; set; }
}

/// <summary>
/// Category-specific notification preferences
/// </summary>
public class CategoryPreferences
{
    public bool? Marketing { get; set; }
    public bool? Social { get; set; }
    public bool? System { get; set; }
    public bool? Subscription { get; set; }
}
