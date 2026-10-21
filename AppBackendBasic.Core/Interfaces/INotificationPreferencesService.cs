using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for managing user notification preferences
/// </summary>
public interface INotificationPreferencesService
{
    /// <summary>
    /// Get user notification preferences
    /// </summary>
    Task<NotificationPreference?> GetPreferencesAsync(int userId);
    
    /// <summary>
    /// Update notification preferences
    /// </summary>
    Task<NotificationPreference> UpdatePreferencesAsync(int userId, NotificationPreferencesUpdate update);
    
    /// <summary>
    /// Create default preferences for new user
    /// </summary>
    Task<NotificationPreference> CreateDefaultPreferencesAsync(int userId);
}

public class NotificationPreferencesUpdate
{
    public bool? PushEnabled { get; set; }
    public bool? EmailEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public bool? Marketing { get; set; }
    public bool? Updates { get; set; }
    public bool? Social { get; set; }
    public bool? Reminders { get; set; }
}
