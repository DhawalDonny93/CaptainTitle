using AppBackendBasic.Core.DTOs;
using AppBackendBasic.Core.Entities;
using AppBackendBasic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.Device;

/// <summary>
/// Service to manage device information during login/registration
/// </summary>
public class DeviceManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeviceManagementService> _logger;

    public DeviceManagementService(
        ApplicationDbContext context,
        ILogger<DeviceManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Register or update device token for push notifications
    /// </summary>
    public async Task<DeviceToken?> RegisterOrUpdateDeviceTokenAsync(
        int userId,
        DeviceInfoDto? deviceInfo,
        string? pushToken = null)
    {
        if (deviceInfo == null)
        {
            _logger.LogDebug("No device info provided for user {UserId}", userId);
            return null;
        }

        try
        {
            // If push token is provided, register it
            if (!string.IsNullOrEmpty(pushToken))
            {
                var existingToken = await _context.Set<DeviceToken>()
                    .FirstOrDefaultAsync(dt => dt.Token == pushToken && dt.UserId == userId);

                if (existingToken != null)
                {
                    // Update existing token
                    existingToken.IsActive = true;
                    existingToken.DeviceModel = deviceInfo.Model;
                    existingToken.AppVersion = deviceInfo.AppVersion;
                    existingToken.LastUsedAt = DateTime.UtcNow;
                    existingToken.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Updated device token for user {UserId}", userId);
                }
                else
                {
                    // Create new token
                    var deviceToken = new DeviceToken
                    {
                        UserId = userId,
                        Token = pushToken,
                        Platform = deviceInfo.Platform ?? "Unknown",
                        DeviceId = deviceInfo.DeviceId,
                        DeviceModel = deviceInfo.Model,
                        AppVersion = deviceInfo.AppVersion,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastUsedAt = DateTime.UtcNow
                    };

                    _context.Set<DeviceToken>().Add(deviceToken);
                    _logger.LogInformation("Created new device token for user {UserId}", userId);
                }

                await _context.SaveChangesAsync();
                return existingToken;
            }
            else if (!string.IsNullOrEmpty(deviceInfo.DeviceId))
            {
                // No push token yet, but we have device info - update existing or wait for token registration
                var existingToken = await _context.Set<DeviceToken>()
                    .FirstOrDefaultAsync(dt => dt.DeviceId == deviceInfo.DeviceId && dt.UserId == userId);

                if (existingToken != null)
                {
                    existingToken.DeviceModel = deviceInfo.Model;
                    existingToken.AppVersion = deviceInfo.AppVersion;
                    existingToken.LastUsedAt = DateTime.UtcNow;
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Updated device info for user {UserId}", userId);
                    return existingToken;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering/updating device token for user {UserId}", userId);
            return null;
        }
    }


    /// <summary>
    /// Get user's active devices
    /// </summary>
    public async Task<List<DeviceInfoSummaryDto>> GetUserDevicesAsync(int userId)
    {
        try
        {
            var devices = await _context.Set<DeviceToken>()
                .Where(dt => dt.UserId == userId && dt.IsActive)
                .Select(dt => new DeviceInfoSummaryDto
                {
                    DeviceId = dt.DeviceId ?? "Unknown",
                    Platform = dt.Platform,
                    Model = dt.DeviceModel ?? "Unknown",
                    AppVersion = dt.AppVersion ?? "Unknown",
                    LastUsed = dt.LastUsedAt ?? dt.CreatedAt,
                    IsActive = dt.IsActive
                })
                .ToListAsync();

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices for user {UserId}", userId);
            return new List<DeviceInfoSummaryDto>();
        }
    }

    /// <summary>
    /// Deactivate device token (e.g., on logout)
    /// </summary>
    public async Task<bool> DeactivateDeviceTokenAsync(int userId, string deviceId)
    {
        try
        {
            var token = await _context.Set<DeviceToken>()
                .FirstOrDefaultAsync(dt => dt.UserId == userId && dt.DeviceId == deviceId);

            if (token != null)
            {
                token.IsActive = false;
                token.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deactivated device token for user {UserId}, device {DeviceId}", 
                    userId, deviceId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating device token for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Clean up old expired sessions
    /// </summary>
    public async Task CleanupOldSessionsAsync(int daysOld = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            
            var oldSessions = await _context.Set<UserSession>()
                .Where(s => s.ExpiresAt < cutoffDate)
                .ToListAsync();

            if (oldSessions.Any())
            {
                _context.Set<UserSession>().RemoveRange(oldSessions);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old sessions", oldSessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old sessions");
        }
    }
}

/// <summary>
/// DTO for device information summary
/// </summary>
public class DeviceInfoSummaryDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public DateTime LastUsed { get; set; }
    public bool IsActive { get; set; }
}
