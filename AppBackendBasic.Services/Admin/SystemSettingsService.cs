using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Admin;

/// <summary>
/// Service for managing system settings
/// </summary>
public class SystemSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(
        ApplicationDbContext context,
        ILogger<SystemSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all settings
    /// </summary>
    public async Task<List<SystemSetting>> GetAllSettingsAsync()
    {
        return await _context.SystemSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    /// <summary>
    /// Get settings by category
    /// </summary>
    public async Task<List<SystemSetting>> GetSettingsByCategoryAsync(string category)
    {
        return await _context.SystemSettings
            .Where(s => s.Category == category)
            .OrderBy(s => s.Key)
            .ToListAsync();
    }

    /// <summary>
    /// Get setting by key
    /// </summary>
    public async Task<SystemSetting?> GetSettingAsync(string key)
    {
        return await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key);
    }

    /// <summary>
    /// Get setting value
    /// </summary>
    public async Task<string?> GetSettingValueAsync(string key)
    {
        var setting = await GetSettingAsync(key);
        return setting?.Value;
    }

    /// <summary>
    /// Get setting value as bool
    /// </summary>
    public async Task<bool> GetBoolSettingAsync(string key, bool defaultValue = false)
    {
        var value = await GetSettingValueAsync(key);
        if (string.IsNullOrEmpty(value))
            return defaultValue;
        
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Get setting value as int
    /// </summary>
    public async Task<int> GetIntSettingAsync(string key, int defaultValue = 0)
    {
        var value = await GetSettingValueAsync(key);
        if (string.IsNullOrEmpty(value))
            return defaultValue;
        
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Create or update setting
    /// </summary>
    public async Task<SystemSetting> UpsertSettingAsync(
        string key,
        string value,
        string type = "string",
        string category = "general",
        string? description = null,
        bool isPublic = false)
    {
        try
        {
            var setting = await GetSettingAsync(key);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Type = type,
                    Category = category,
                    Description = description,
                    IsPublic = isPublic,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SystemSettings.Add(setting);
                _logger.LogInformation("Created new setting: {Key}", key);
            }
            else
            {
                setting.Value = value;
                setting.Type = type;
                setting.Category = category;
                setting.Description = description;
                setting.IsPublic = isPublic;
                setting.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Updated setting: {Key}", key);
            }

            await _context.SaveChangesAsync();
            return setting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting setting {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Delete setting
    /// </summary>
    public async Task<bool> DeleteSettingAsync(string key)
    {
        try
        {
            var setting = await GetSettingAsync(key);
            if (setting == null)
                return false;

            _context.SystemSettings.Remove(setting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted setting: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Seed default settings
    /// </summary>
    public async Task SeedDefaultSettingsAsync()
    {
        try
        {
            var defaultSettings = new (string Key, string Value, string Type, string Category, string Description, bool IsPublic)[]
            {
                (SettingKeys.MaintenanceMode, "false", "bool", "features", "Enable maintenance mode to block all API access", false),
                (SettingKeys.RegistrationEnabled, "true", "bool", "features", "Allow new user registrations", false),
                (SettingKeys.SocialLoginEnabled, "true", "bool", "features", "Enable social login (Google, Apple)", false),
                (SettingKeys.SubscriptionsEnabled, "true", "bool", "features", "Enable subscription purchases", false),
                (SettingKeys.LoginRateLimit, "5", "int", "limits", "Login attempts per minute per IP", false),
                (SettingKeys.RegistrationRateLimit, "3", "int", "limits", "Registration attempts per minute per IP", false),
                (SettingKeys.ApiRateLimit, "100", "int", "limits", "API requests per minute per IP", false),
                (SettingKeys.PushNotificationsEnabled, "true", "bool", "notifications", "Enable push notifications", false),
                (SettingKeys.EmailNotificationsEnabled, "true", "bool", "notifications", "Enable email notifications", false),
                (SettingKeys.InAppNotificationsEnabled, "true", "bool", "notifications", "Enable in-app notifications", false),
                (SettingKeys.AppName, "MyApp", "string", "app", "Application name", true),
                (SettingKeys.AppVersion, "1.0.0", "string", "app", "Current app version", true),
                (SettingKeys.MinAppVersion, "1.0.0", "string", "app", "Minimum required app version", true)
            };

            foreach (var defaultSetting in defaultSettings)
            {
                var exists = await _context.SystemSettings.AnyAsync(s => s.Key == defaultSetting.Key);
                if (!exists)
                {
                    var setting = new SystemSetting
                    {
                        Key = defaultSetting.Key,
                        Value = defaultSetting.Value,
                        Type = defaultSetting.Type,
                        Category = defaultSetting.Category,
                        Description = defaultSetting.Description,
                        IsPublic = defaultSetting.IsPublic,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SystemSettings.Add(setting);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Default settings seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default settings");
            throw;
        }
    }
}
