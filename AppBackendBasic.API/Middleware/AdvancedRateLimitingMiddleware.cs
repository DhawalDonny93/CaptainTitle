using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace AppBackendBasic.API.Middleware;

public class AdvancedRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdvancedRateLimitingMiddleware> _logger;
    
    // Configuration
    private readonly int _loginAttemptsLimit = 5;
    private readonly int _loginAttemptsWindow = 15; // minutes
    private readonly int _apiRequestsPerMinute = 60;
    private readonly int _apiRequestsPerHour = 1000;
    
    // Track failed login attempts for exponential backoff
    private static readonly ConcurrentDictionary<string, FailedLoginAttempts> _failedLogins = new();

    public AdvancedRateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<AdvancedRateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Request.Path.Value?.ToLower() ?? string.Empty;
        var ipAddress = GetClientIpAddress(context);
        
        // Special handling for login endpoint
        if (endpoint.Contains("/auth/login"))
        {
            if (!await CheckLoginRateLimitAsync(ipAddress))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Too many login attempts. Please try again later.",
                    retryAfter = GetLoginRetryAfter(ipAddress)
                });
                return;
            }
        }
        // General API rate limiting for authenticated users
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                if (!await CheckApiRateLimitAsync(userId))
                {
                    context.Response.StatusCode = 429;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "API rate limit exceeded. Please slow down.",
                        retryAfter = 60
                    });
                    return;
                }
            }
        }

        await _next(context);

        // Track failed login attempts
        if (endpoint.Contains("/auth/login") && context.Response.StatusCode == 401)
        {
            await RecordFailedLoginAsync(ipAddress);
        }
        else if (endpoint.Contains("/auth/login") && context.Response.StatusCode == 200)
        {
            // Clear failed attempts on successful login
            _failedLogins.TryRemove(ipAddress, out _);
        }
    }

    private async Task<bool> CheckLoginRateLimitAsync(string ipAddress)
    {
        var cacheKey = $"login_attempts_{ipAddress}";
        
        if (_failedLogins.TryGetValue(ipAddress, out var attempts))
        {
            // Exponential backoff
            if (attempts.Count >= _loginAttemptsLimit)
            {
                var backoffMinutes = Math.Pow(2, attempts.Count - _loginAttemptsLimit);
                var lockoutUntil = attempts.LastAttempt.AddMinutes(backoffMinutes);
                
                if (DateTime.UtcNow < lockoutUntil)
                {
                    _logger.LogWarning("Login attempt blocked for IP {IpAddress} due to exponential backoff", ipAddress);
                    return false;
                }
                else
                {
                    // Backoff period expired, reset
                    _failedLogins.TryRemove(ipAddress, out _);
                }
            }
        }

        // Check sliding window
        if (!_cache.TryGetValue(cacheKey, out List<DateTime> loginAttempts))
        {
            loginAttempts = new List<DateTime>();
        }

        // Remove old attempts outside the window
        var windowStart = DateTime.UtcNow.AddMinutes(-_loginAttemptsWindow);
        loginAttempts = loginAttempts.Where(t => t > windowStart).ToList();

        if (loginAttempts.Count >= _loginAttemptsLimit)
        {
            _logger.LogWarning("Login rate limit exceeded for IP {IpAddress}", ipAddress);
            return false;
        }

        // Add current attempt
        loginAttempts.Add(DateTime.UtcNow);
        _cache.Set(cacheKey, loginAttempts, TimeSpan.FromMinutes(_loginAttemptsWindow));

        return true;
    }

    private async Task<bool> CheckApiRateLimitAsync(string userId)
    {
        var now = DateTime.UtcNow;
        
        // Check requests per minute
        var minuteKey = $"api_rpm_{userId}";
        if (!_cache.TryGetValue(minuteKey, out int requestsPerMinute))
        {
            requestsPerMinute = 0;
        }

        if (requestsPerMinute >= _apiRequestsPerMinute)
        {
            _logger.LogWarning("API rate limit (per minute) exceeded for user {UserId}", userId);
            return false;
        }

        // Check requests per hour
        var hourKey = $"api_rph_{userId}";
        if (!_cache.TryGetValue(hourKey, out int requestsPerHour))
        {
            requestsPerHour = 0;
        }

        if (requestsPerHour >= _apiRequestsPerHour)
        {
            _logger.LogWarning("API rate limit (per hour) exceeded for user {UserId}", userId);
            return false;
        }

        // Increment counters
        _cache.Set(minuteKey, requestsPerMinute + 1, TimeSpan.FromMinutes(1));
        _cache.Set(hourKey, requestsPerHour + 1, TimeSpan.FromHours(1));

        return true;
    }

    private async Task RecordFailedLoginAsync(string ipAddress)
    {
        _failedLogins.AddOrUpdate(
            ipAddress,
            new FailedLoginAttempts { Count = 1, LastAttempt = DateTime.UtcNow },
            (key, existing) => new FailedLoginAttempts
            {
                Count = existing.Count + 1,
                LastAttempt = DateTime.UtcNow
            });

        _logger.LogWarning("Failed login attempt recorded for IP {IpAddress}", ipAddress);
    }

    private int GetLoginRetryAfter(string ipAddress)
    {
        if (_failedLogins.TryGetValue(ipAddress, out var attempts) && attempts.Count >= _loginAttemptsLimit)
        {
            var backoffMinutes = Math.Pow(2, attempts.Count - _loginAttemptsLimit);
            var lockoutUntil = attempts.LastAttempt.AddMinutes(backoffMinutes);
            var remainingSeconds = (int)(lockoutUntil - DateTime.UtcNow).TotalSeconds;
            return Math.Max(remainingSeconds, 0);
        }

        return 60; // Default 1 minute
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Try to get real IP from headers (for reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class FailedLoginAttempts
    {
        public int Count { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}

public static class AdvancedRateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseAdvancedRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdvancedRateLimitingMiddleware>();
    }
}
