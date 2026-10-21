using AppBackendBasic.Core.Entities;
using AppBackendBasic.Core.Interfaces;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Notifications;

public class NotificationPreferencesService : INotificationPreferencesService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationPreferencesService> _logger;

    public NotificationPreferencesService(
        ApplicationDbContext context,
        ILogger<NotificationPreferencesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationPreference?> GetPreferencesAsync(int userId)
    {
        try
        {
            var preferences = await _context.Set<NotificationPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // Create default if doesn't exist
            if (preferences == null)
            {
                preferences = await CreateDefaultPreferencesAsync(userId);
            }

            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<NotificationPreference> UpdatePreferencesAsync(
        int userId, 
        NotificationPreferencesUpdate update)
    {
        try
        {
            var preferences = await _context.Set<NotificationPreference>()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (preferences == null)
            {
                preferences = await CreateDefaultPreferencesAsync(userId);
            }

            // Update only provided fields
            if (update.PushEnabled.HasValue)
                preferences.PushEnabled = update.PushEnabled.Value;
            
            if (update.EmailEnabled.HasValue)
                preferences.EmailEnabled = update.EmailEnabled.Value;
            
            if (update.SmsEnabled.HasValue)
                preferences.SmsEnabled = update.SmsEnabled.Value;
            
            if (update.Marketing.HasValue)
                preferences.MarketingNotifications = update.Marketing.Value;
            
            if (update.Social.HasValue)
                preferences.SubscriptionNotifications = update.Social.Value;
            
            // Security notifications always enabled
            preferences.SystemNotifications = true;
            
            preferences.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification preferences updated for user {UserId}", userId);

            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user {UserId}", userId);
            throw;
        }
    }

    public async Task<NotificationPreference> CreateDefaultPreferencesAsync(int userId)
    {
        try
        {
            var preferences = new NotificationPreference
            {
                UserId = userId,
                PushEnabled = true,
                EmailEnabled = true,
                SmsEnabled = false,
                MarketingNotifications = false,
                SystemNotifications = true,
                SocialNotifications = true,
                SubscriptionNotifications = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<NotificationPreference>().Add(preferences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Default notification preferences created for user {UserId}", userId);

            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default preferences for user {UserId}", userId);
            throw;
        }
    }
}
