using System.ComponentModel.DataAnnotations;

namespace AppBackendBasic.Core.Entities;

/// <summary>
/// System configuration settings
/// </summary>
public class SystemSetting
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Type { get; set; } = "string"; // string, int, bool, json
    
    [MaxLength(100)]
    public string Category { get; set; } = "general"; // general, features, limits, email, notifications
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsPublic { get; set; } = false; // Can be accessed by non-admin users
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Predefined setting keys
/// </summary>
public static class SettingKeys
{
    // Feature Flags
    public const string MaintenanceMode = "feature.maintenance_mode";
    public const string RegistrationEnabled = "feature.registration_enabled";
    public const string SocialLoginEnabled = "feature.social_login_enabled";
    public const string SubscriptionsEnabled = "feature.subscriptions_enabled";
    
    // Rate Limits
    public const string LoginRateLimit = "limits.login_per_minute";
    public const string RegistrationRateLimit = "limits.registration_per_minute";
    public const string ApiRateLimit = "limits.api_per_minute";
    
    // Email Settings
    public const string EmailFromName = "email.from_name";
    public const string EmailReplyTo = "email.reply_to";
    public const string EmailFooter = "email.footer";
    
    // Notification Settings
    public const string PushNotificationsEnabled = "notifications.push_enabled";
    public const string EmailNotificationsEnabled = "notifications.email_enabled";
    public const string InAppNotificationsEnabled = "notifications.in_app_enabled";
    
    // App Settings
    public const string AppName = "app.name";
    public const string AppVersion = "app.version";
    public const string MinAppVersion = "app.min_version";
}
