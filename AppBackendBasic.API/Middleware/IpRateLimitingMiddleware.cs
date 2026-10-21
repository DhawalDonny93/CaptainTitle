using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace AppBackendBasic.API.Middleware;

public class IpRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<IpRateLimitingMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public IpRateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<IpRateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if IP rate limiting is enabled
        var enabled = _configuration.GetValue<bool>("RateLimiting:EnableIpRateLimiting", true);
        if (!enabled)
        {
            await _next(context);
            return;
        }

        // Get client IP address
        var ipAddress = GetClientIpAddress(context);

        // Check if IP is whitelisted
        var whitelist = _configuration.GetSection("RateLimiting:IpWhitelist").Get<string[]>() ?? Array.Empty<string>();
        if (whitelist.Contains(ipAddress))
        {
            await _next(context);
            return;
        }

        // Get rate limit rules
        var endpoint = $"{context.Request.Method}:{context.Request.Path}";
        var (limit, periodSeconds) = GetRateLimitForEndpoint(endpoint);

        // Create cache key
        var cacheKey = $"rate_limit_ip:{ipAddress}:{endpoint}";

        // Get or create rate limit data
        var rateLimitData = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(periodSeconds);
            return new RateLimitData
            {
                Count = 0,
                ResetTime = DateTime.UtcNow.AddSeconds(periodSeconds)
            };
        });

        // Increment request count
        rateLimitData!.Count++;
        _cache.Set(cacheKey, rateLimitData, TimeSpan.FromSeconds(periodSeconds));

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - rateLimitData.Count).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = new DateTimeOffset(rateLimitData.ResetTime).ToUnixTimeSeconds().ToString();

        // Check if rate limit exceeded
        if (rateLimitData.Count > limit)
        {
            var retryAfter = (int)(rateLimitData.ResetTime - DateTime.UtcNow).TotalSeconds;
            context.Response.Headers["Retry-After"] = retryAfter.ToString();

            _logger.LogWarning(
                "Rate limit exceeded for IP {IpAddress} on endpoint {Endpoint}. Count: {Count}/{Limit}",
                ipAddress, endpoint, rateLimitData.Count, limit);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too Many Requests",
                message = $"Rate limit exceeded. Please try again in {retryAfter} seconds.",
                retryAfter = retryAfter,
                limit = limit,
                resetTime = rateLimitData.ResetTime
            });
            return;
        }

        await _next(context);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check X-Real-IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private (int limit, int periodSeconds) GetRateLimitForEndpoint(string endpoint)
    {
        // Check for specific endpoint rules
        var endpointRules = _configuration.GetSection("RateLimiting:EndpointRules").Get<EndpointRule[]>();
        if (endpointRules != null)
        {
            foreach (var rule in endpointRules)
            {
                if (EndpointMatches(endpoint, rule.Endpoint))
                {
                    return (rule.Limit, ParsePeriod(rule.Period));
                }
            }
        }

        // Use general rules as fallback
        var generalLimit = _configuration.GetValue<int>("RateLimiting:GeneralRules:Limit", 100);
        var generalPeriod = _configuration.GetValue<string>("RateLimiting:GeneralRules:Period", "1m");
        return (generalLimit, ParsePeriod(generalPeriod));
    }

    private bool EndpointMatches(string actual, string pattern)
    {
        // Simple wildcard matching
        if (pattern == "*") return true;
        
        // Exact match
        if (actual.Equals(pattern, StringComparison.OrdinalIgnoreCase)) return true;
        
        // Pattern matching with wildcards
        if (pattern.Contains("*"))
        {
            var regex = new System.Text.RegularExpressions.Regex(
                "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return regex.IsMatch(actual);
        }

        return false;
    }

    private int ParsePeriod(string period)
    {
        // Parse period string (e.g., "1m", "1h", "30s")
        if (string.IsNullOrEmpty(period)) return 60;

        var unit = period[^1];
        var value = int.Parse(period[..^1]);

        return unit switch
        {
            's' => value,
            'm' => value * 60,
            'h' => value * 3600,
            'd' => value * 86400,
            _ => 60
        };
    }

    private class RateLimitData
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }

    private class EndpointRule
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Period { get; set; } = "1m";
        public int Limit { get; set; } = 100;
    }
}

public static class IpRateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseIpRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IpRateLimitingMiddleware>();
    }
}
