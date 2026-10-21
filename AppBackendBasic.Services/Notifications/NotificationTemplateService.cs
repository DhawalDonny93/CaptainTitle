using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Notifications;

/// <summary>
/// Service for sending common notification templates
/// </summary>
public class NotificationTemplateService
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly INotificationPreferencesService _preferencesService;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(
        IPushNotificationService pushNotificationService,
        INotificationPreferencesService preferencesService,
        ILogger<NotificationTemplateService> logger)
    {
        _pushNotificationService = pushNotificationService;
        _preferencesService = preferencesService;
        _logger = logger;
    }

    /// <summary>
    /// Send welcome notification to new user
    /// </summary>
    public async Task<bool> SendWelcomeNotificationAsync(int userId, string firstName)
    {
        try
        {
            var preferences = await _preferencesService.GetPreferencesAsync(userId);
            if (preferences?.PushEnabled != true)
                return false;

            var message = new PushNotificationMessage
            {
                Title = "Welcome! 🎉",
                Body = $"Hi {firstName}! Thanks for joining us. Let's get started!",
                Sound = "default",
                Category = "welcome",
                Data = new Dictionary<string, string>
                {
                    { "type", "welcome" },
                    { "action", "open_onboarding" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome notification to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Send subscription expiring notification
    /// </summary>
    public async Task<bool> SendSubscriptionExpiringNotificationAsync(int userId, int daysRemaining)
    {
        try
        {
            // Security notifications always go through
            var message = new PushNotificationMessage
            {
                Title = "Subscription Expiring Soon ⏰",
                Body = $"Your subscription expires in {daysRemaining} days. Renew now to keep your access!",
                Sound = "default",
                Category = "subscription",
                Badge = "1",
                Data = new Dictionary<string, string>
                {
                    { "type", "subscription_expiring" },
                    { "daysRemaining", daysRemaining.ToString() },
                    { "action", "open_subscription" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending subscription expiring notification to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Send account deletion reminder
    /// </summary>
    public async Task<bool> SendAccountDeletionReminderAsync(int userId, int daysRemaining)
    {
        try
        {
            // Security notifications always go through
            var message = new PushNotificationMessage
            {
                Title = "Account Deletion Scheduled ⚠️",
                Body = $"Your account will be permanently deleted in {daysRemaining} days. Cancel anytime in settings.",
                Sound = "default",
                Category = "account_deletion",
                Badge = "1",
                Data = new Dictionary<string, string>
                {
                    { "type", "account_deletion" },
                    { "daysRemaining", daysRemaining.ToString() },
                    { "action", "open_settings" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending account deletion reminder to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Send data export ready notification
    /// </summary>
    public async Task<bool> SendDataExportReadyNotificationAsync(int userId)
    {
        try
        {
            // Security notifications always go through
            var message = new PushNotificationMessage
            {
                Title = "Your Data Export is Ready 📦",
                Body = "Your requested data export is now available for download. Link expires in 24 hours.",
                Sound = "default",
                Category = "data_export",
                Badge = "1",
                Data = new Dictionary<string, string>
                {
                    { "type", "data_export_ready" },
                    { "action", "open_data_export" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data export ready notification to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Send onboarding completed notification
    /// </summary>
    public async Task<bool> SendOnboardingCompletedNotificationAsync(int userId, string firstName)
    {
        try
        {
            var preferences = await _preferencesService.GetPreferencesAsync(userId);
            if (preferences?.PushEnabled != true)
                return false;

            var message = new PushNotificationMessage
            {
                Title = "You're All Set! ✨",
                Body = $"Great job, {firstName}! You've completed onboarding. Start exploring now!",
                Sound = "default",
                Category = "onboarding",
                Data = new Dictionary<string, string>
                {
                    { "type", "onboarding_complete" },
                    { "action", "open_home" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending onboarding completed notification to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Send security alert notification
    /// </summary>
    public async Task<bool> SendSecurityAlertAsync(int userId, string alertMessage)
    {
        try
        {
            // Security notifications always go through (cannot be disabled)
            var message = new PushNotificationMessage
            {
                Title = "Security Alert 🔒",
                Body = alertMessage,
                Sound = "default",
                Category = "security",
                Badge = "1",
                Data = new Dictionary<string, string>
                {
                    { "type", "security_alert" },
                    { "action", "open_security_settings" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security alert to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Send password changed notification
    /// </summary>
    public async Task<bool> SendPasswordChangedNotificationAsync(int userId)
    {
        try
        {
            // Security notifications always go through
            var message = new PushNotificationMessage
            {
                Title = "Password Changed 🔐",
                Body = "Your password was recently changed. If this wasn't you, please contact support immediately.",
                Sound = "default",
                Category = "security",
                Badge = "1",
                Data = new Dictionary<string, string>
                {
                    { "type", "password_changed" },
                    { "action", "open_security_settings" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password changed notification to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Send new login notification
    /// </summary>
    public async Task<bool> SendNewLoginNotificationAsync(int userId, string deviceInfo, string location)
    {
        try
        {
            // Security notifications always go through
            var message = new PushNotificationMessage
            {
                Title = "New Login Detected 📱",
                Body = $"New login from {deviceInfo} in {location}. If this wasn't you, secure your account immediately.",
                Sound = "default",
                Category = "security",
                Badge = "1",
                Data = new Dictionary<string, string>
                {
                    { "type", "new_login" },
                    { "device", deviceInfo },
                    { "location", location },
                    { "action", "open_security_settings" }
                }
            };

            var result = await _pushNotificationService.SendNotificationAsync(userId, message);
            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new login notification to user {UserId}", userId);
            return false;
        }
    }
}
