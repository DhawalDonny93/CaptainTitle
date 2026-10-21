using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AppBackendBasic.Services.Caching;

/// <summary>
/// Redis cache service implementation
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(
        IDistributedCache cache,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(cachedData))
                return null;

            return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached data for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration: 5 minutes
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            }

            await _cache.SetStringAsync(key, serializedData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached data for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached data for key {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists {Key}", key);
            return false;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            // Note: Pattern-based removal requires Redis-specific implementation
            // This is a simplified version that logs a warning
            _logger.LogWarning("Pattern-based cache removal not fully implemented for key pattern {Pattern}", pattern);
            
            // In a full implementation, you would:
            // 1. Get IConnectionMultiplexer from DI
            // 2. Use SCAN command to find matching keys
            // 3. Delete matching keys
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached data by pattern {Pattern}", pattern);
        }
    }

    /// <summary>
    /// Invalidate user-related cache entries
    /// </summary>
    public async Task InvalidateUserCacheAsync(int userId)
    {
        try
        {
            await RemoveAsync(CacheKeys.User(userId));
            await RemoveAsync(CacheKeys.UserProfile(userId));
            await RemoveAsync(CacheKeys.UserRoles(userId));
            await RemoveAsync(CacheKeys.UserSessions(userId));
            await RemoveAsync(CacheKeys.UserSubscription(userId));
            await RemoveAsync(CacheKeys.NotificationPreferences(userId));
            await RemoveAsync(CacheKeys.OnboardingProgress(userId));
            await RemoveAsync(CacheKeys.OnboardingAnswers(userId));
            
            _logger.LogDebug("Invalidated cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user cache for {UserId}", userId);
        }
    }

    /// <summary>
    /// Invalidate system settings cache
    /// </summary>
    public async Task InvalidateSystemSettingsCacheAsync()
    {
        try
        {
            await RemoveAsync(CacheKeys.AllSystemSettings());
            _logger.LogDebug("Invalidated system settings cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating system settings cache");
        }
    }

    /// <summary>
    /// Invalidate analytics cache
    /// </summary>
    public async Task InvalidateAnalyticsCacheAsync()
    {
        try
        {
            await RemoveAsync(CacheKeys.DashboardStats());
            _logger.LogDebug("Invalidated analytics cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating analytics cache");
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        try
        {
            // Try to get from cache
            var cachedData = await GetAsync<T>(key);
            
            if (cachedData != null)
            {
                _logger.LogDebug("Cache hit for key {Key}", key);
                return cachedData;
            }

            // Cache miss - get from factory
            _logger.LogDebug("Cache miss for key {Key}, fetching from source", key);
            var data = await factory();
            
            // Set in cache
            await SetAsync(key, data, expiration);
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSet for key {Key}", key);
            
            // On error, try to get data from factory without caching
            return await factory();
        }
    }
}
