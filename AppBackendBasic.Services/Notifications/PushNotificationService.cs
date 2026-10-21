using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using dotAPNS;
using System.Security.Cryptography;

namespace AppBackendBasic.Services.Notifications;

public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly IHostEnvironment _environment;
    private readonly ExpoPushNotificationService _expoService;
    private readonly ApnsClient? _apnsClient;
    
    // APNs configuration
    private readonly string? _apnsKeyId;
    private readonly string? _apnsTeamId;
    private readonly string? _apnsBundleId;
    private readonly string? _apnsKeyPath;
    private readonly bool _apnsUseSandbox;

    public PushNotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<PushNotificationService> logger,
        IInAppNotificationService inAppNotificationService,
        IHostEnvironment environment,
        ExpoPushNotificationService expoService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _inAppNotificationService = inAppNotificationService;
        _environment = environment;
        _expoService = expoService;
        
        // Load APNs configuration
        _apnsKeyId = _configuration["APNs:KeyId"];
        _apnsTeamId = _configuration["APNs:TeamId"];
        _apnsBundleId = _configuration["APNs:BundleId"];
        var configuredKeyPath = _configuration["APNs:KeyPath"];
        _apnsUseSandbox = _configuration.GetValue<bool>("APNs:UseSandbox", true);
        
        // Resolve the APNS key path relative to the content root
        if (!string.IsNullOrEmpty(configuredKeyPath))
        {
            // If path is relative, resolve it from the content root (where the API project is)
            if (!Path.IsPathRooted(configuredKeyPath))
            {
                _apnsKeyPath = Path.Combine(_environment.ContentRootPath, configuredKeyPath);
            }
            else
            {
                _apnsKeyPath = configuredKeyPath;
            }
        }
        
        // Initialize APNs client if configuration is valid
        if (!string.IsNullOrEmpty(_apnsKeyId) && 
            !string.IsNullOrEmpty(_apnsTeamId) && 
            !string.IsNullOrEmpty(_apnsBundleId) &&
            !string.IsNullOrEmpty(_apnsKeyPath) &&
            File.Exists(_apnsKeyPath))
        {
            try
            {
                var p8Key = File.ReadAllText(_apnsKeyPath);
                var jwt = new ApnsJwtOptions
                {
                    KeyId = _apnsKeyId,
                    TeamId = _apnsTeamId,
                    BundleId = _apnsBundleId,
                    CertContent = p8Key
                };
                
                var httpClient = new System.Net.Http.HttpClient();
                
                // Note: dotAPNS automatically determines sandbox vs production based on the device token
                // The UseSandbox setting in config is just for logging/reference
                // The actual environment is determined by Apple based on how the app was built
                _apnsClient = ApnsClient.CreateUsingJwt(httpClient, jwt);
                    
                _logger.LogInformation("APNs client initialized successfully (Sandbox: {IsSandbox})", _apnsUseSandbox);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize APNs client");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_apnsKeyPath) && !File.Exists(_apnsKeyPath))
            {
                _logger.LogWarning("APNs key file not found at path: {Path}. Checked: {FullPath}", 
                    configuredKeyPath, _apnsKeyPath);
            }
            else
            {
                _logger.LogWarning("APNs not configured. Push notifications will not be sent.");
            }
        }
    }

    public async Task<bool> RegisterDeviceTokenAsync(
        int userId, 
        string token, 
        string platform, 
        string? deviceId = null, 
        string? deviceModel = null, 
        string? appVersion = null)
    {
        try
        {
            // Detect token type (Expo or Native)
            var tokenType = DetectTokenType(token);
            
            _logger.LogInformation("Registering {TokenType} token for user {UserId} on platform {Platform}", 
                tokenType, userId, platform);
            
            // Check if token already exists
            var existingToken = await _context.Set<DeviceToken>()
                .FirstOrDefaultAsync(dt => dt.Token == token && dt.UserId == userId);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.IsActive = true;
                existingToken.TokenType = tokenType;
                existingToken.UpdatedAt = DateTime.UtcNow;
                existingToken.LastUsedAt = DateTime.UtcNow;
                existingToken.DeviceModel = deviceModel ?? existingToken.DeviceModel;
                existingToken.AppVersion = appVersion ?? existingToken.AppVersion;
            }
            else
            {
                // Create new token
                var deviceToken = new DeviceToken
                {
                    UserId = userId,
                    Token = token,
                    Platform = platform,
                    TokenType = tokenType,
                    DeviceId = deviceId,
                    DeviceModel = deviceModel,
                    AppVersion = appVersion,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow
                };

                _context.Set<DeviceToken>().Add(deviceToken);
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device token registered for user {UserId}, platform {Platform}", userId, platform);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnregisterDeviceTokenAsync(int userId, string token)
    {
        try
        {
            var deviceToken = await _context.Set<DeviceToken>()
                .FirstOrDefaultAsync(dt => dt.Token == token && dt.UserId == userId);

            if (deviceToken != null)
            {
                deviceToken.IsActive = false;
                deviceToken.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Device token unregistered for user {UserId}", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device token for user {UserId}", userId);
            return false;
        }
    }

    public async Task<PushNotificationResult> SendNotificationAsync(int userId, PushNotificationMessage message)
    {
        try
        {
            var tokens = await GetUserDeviceTokensAsync(userId);
            
            if (!tokens.Any())
            {
                _logger.LogWarning("No active device tokens found for user {UserId}", userId);
                return new PushNotificationResult
                {
                    Success = false,
                    ErrorMessage = "No active device tokens",
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            // Get unread notification count for badge
            if (string.IsNullOrEmpty(message.Badge))
            {
                var unreadCount = await _inAppNotificationService.GetUnreadCountAsync(userId);
                message.Badge = unreadCount.ToString();
            }

            int successCount = 0;
            int failureCount = 0;

            // Group tokens by type
            var tokensByType = await _context.Set<DeviceToken>()
                .Where(dt => tokens.Contains(dt.Token) && dt.UserId == userId && dt.IsActive)
                .ToListAsync();

            foreach (var deviceToken in tokensByType)
            {
                bool result;
                
                if (deviceToken.TokenType == "Expo")
                {
                    result = await SendToExpoAsync(deviceToken.Token, message);
                }
                else
                {
                    result = await SendToAPNsAsync(deviceToken.Token, message);
                }
                
                if (result)
                    successCount++;
                else
                    failureCount++;
            }

            _logger.LogInformation("Sent notifications to user {UserId}: {Success} succeeded, {Failed} failed", 
                userId, successCount, failureCount);

            return new PushNotificationResult
            {
                Success = successCount > 0,
                SuccessCount = successCount,
                FailureCount = failureCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SuccessCount = 0,
                FailureCount = 1
            };
        }
    }

    public async Task<PushNotificationResult> SendNotificationToDeviceAsync(
        string token, 
        string platform, 
        PushNotificationMessage message)
    {
        try
        {
            bool success = false;

            if (platform.ToLower() == "ios")
            {
                success = await SendToAPNsAsync(token, message);
            }
            else if (platform.ToLower() == "android")
            {
                // FCM implementation would go here
                _logger.LogWarning("FCM not yet implemented");
                success = false;
            }

            return new PushNotificationResult
            {
                Success = success,
                SuccessCount = success ? 1 : 0,
                FailureCount = success ? 0 : 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to device");
            return new PushNotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SuccessCount = 0,
                FailureCount = 1
            };
        }
    }

    public async Task<List<PushNotificationResult>> SendBulkNotificationAsync(
        List<int> userIds, 
        PushNotificationMessage message)
    {
        var results = new List<PushNotificationResult>();

        foreach (var userId in userIds)
        {
            var result = await SendNotificationAsync(userId, message);
            results.Add(result);
        }

        return results;
    }

    public async Task<List<string>> GetUserDeviceTokensAsync(int userId, string? platform = null)
    {
        var query = _context.Set<DeviceToken>()
            .Where(dt => dt.UserId == userId && dt.IsActive);

        if (!string.IsNullOrEmpty(platform))
        {
                query = query.Where(dt => dt.Platform == platform);
        }

        var tokens = await query.Select(dt => dt.Token).ToListAsync();
        
        return tokens;
    }

    private async Task<bool> SendToAPNsAsync(string deviceToken, PushNotificationMessage message)
    {
        try
        {
            // Check if APNs client is initialized
            if (_apnsClient == null)
            {
                _logger.LogWarning("APNs client not initialized. Skipping notification.");
                return false;
            }

            // Build the APNs push notification
            var push = new ApplePush(ApplePushType.Alert)
                .AddToken(deviceToken)
                .AddAlert(message.Title, message.Body)
                .AddSound(message.Sound ?? "default");

            // Add badge if specified
            if (!string.IsNullOrEmpty(message.Badge) && int.TryParse(message.Badge, out var badgeCount))
            {
                push.AddBadge(badgeCount);
            }

            // Add custom data
            if (message.Data != null && message.Data.Any())
            {
                _logger.LogDebug("Adding {Count} custom properties to APNs push", message.Data.Count);
                foreach (var kvp in message.Data)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        push.AddCustomProperty(kvp.Key, kvp.Value);
                        _logger.LogDebug("Added custom property: {Key} = {Value}", kvp.Key, kvp.Value);
                    }
                }
            }
            else
            {
                _logger.LogDebug("No custom data to add to APNs push");
            }

            // Add category if specified
            if (!string.IsNullOrEmpty(message.Category))
            {
                push.AddCategory(message.Category);
            }

            // Add content-available flag if specified
            if (message.ContentAvailable.HasValue && message.ContentAvailable.Value)
            {
                push.AddContentAvailable();
            }

            // Send the notification
            var response = await _apnsClient.SendAsync(push);

            if (response.IsSuccessful)
            {
                _logger.LogInformation("APNs notification sent successfully to token: {Token}", 
                    deviceToken.Substring(0, Math.Min(10, deviceToken.Length)) + "...");
                return true;
            }
            else
            {
                _logger.LogWarning("APNs notification failed. Reason: {Reason}, Token: {Token}", 
                    response.ReasonString, 
                    deviceToken.Substring(0, Math.Min(10, deviceToken.Length)) + "...");
                
                // If token is invalid, deactivate it
                if (response.Reason == ApnsResponseReason.BadDeviceToken || 
                    response.Reason == ApnsResponseReason.Unregistered)
                {
                    await DeactivateInvalidTokenAsync(deviceToken);
                }
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending APNs notification to token: {Token}", 
                deviceToken.Substring(0, Math.Min(10, deviceToken.Length)) + "...");
            return false;
        }
    }

    private async Task<bool> SendToExpoAsync(string expoToken, PushNotificationMessage message)
    {
        try
        {
            var result = await _expoService.SendNotificationAsync(
                expoToken,
                message.Title,
                message.Body,
                message.Data,
                message.Sound,
                !string.IsNullOrEmpty(message.Badge) && int.TryParse(message.Badge, out var badge) ? badge : null
            );

            if (result.Success)
            {
                _logger.LogInformation("Expo notification sent successfully. Ticket: {TicketId}", result.TicketId);
                return true;
            }
            else
            {
                _logger.LogWarning("Expo notification failed. Error: {Error}", result.ErrorMessage);
                
                if (result.ShouldDeactivateToken)
                {
                    await DeactivateInvalidTokenAsync(expoToken);
                }
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Expo notification");
            return false;
        }
    }

    private async Task DeactivateInvalidTokenAsync(string token)
    {
        try
        {
            var deviceToken = await _context.Set<DeviceToken>()
                .FirstOrDefaultAsync(dt => dt.Token == token);

            if (deviceToken != null)
            {
                deviceToken.IsActive = false;
                deviceToken.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deactivated invalid device token: {Token}", 
                    token.Substring(0, Math.Min(10, token.Length)) + "...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating invalid token");
        }
    }

    private string DetectTokenType(string token)
    {
        // Expo tokens start with ExponentPushToken[ or ExpoPushToken[
        if (token.StartsWith("ExponentPushToken[") || token.StartsWith("ExpoPushToken["))
        {
            return "Expo";
        }
        
        // Native tokens are typically 64 hex characters
        return "Native";
    }
}
