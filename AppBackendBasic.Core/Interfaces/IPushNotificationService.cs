namespace AppBackendBasic.Core.Interfaces;

/// <summary>
/// Service for sending push notifications
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Register device token for push notifications
    /// </summary>
    Task<bool> RegisterDeviceTokenAsync(int userId, string token, string platform, string? deviceId = null, string? deviceModel = null, string? appVersion = null);
    
    /// <summary>
    /// Unregister device token
    /// </summary>
    Task<bool> UnregisterDeviceTokenAsync(int userId, string token);
    
    /// <summary>
    /// Send push notification to a user
    /// </summary>
    Task<PushNotificationResult> SendNotificationAsync(int userId, PushNotificationMessage message);
    
    /// <summary>
    /// Send push notification to specific device
    /// </summary>
    Task<PushNotificationResult> SendNotificationToDeviceAsync(string token, string platform, PushNotificationMessage message);
    
    /// <summary>
    /// Send push notification to multiple users
    /// </summary>
    Task<List<PushNotificationResult>> SendBulkNotificationAsync(List<int> userIds, PushNotificationMessage message);
    
    /// <summary>
    /// Get user's active device tokens
    /// </summary>
    Task<List<string>> GetUserDeviceTokensAsync(int userId, string? platform = null);
}

public class PushNotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public string? Badge { get; set; }
    public string? Sound { get; set; } = "default";
    public string? Category { get; set; }
    public bool? ContentAvailable { get; set; }
}

public class PushNotificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
