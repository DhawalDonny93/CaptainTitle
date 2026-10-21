using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppBackendBasic.Infrastructure.Data;
using AppBackendBasic.Core.Entities;

namespace AppBackendBasic.API.Middleware;

public class SessionTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionTimeoutMiddleware> _logger;

    public SessionTimeoutMiddleware(RequestDelegate next, ILogger<SessionTimeoutMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, IConfiguration configuration)
    {
        // Skip for non-authenticated requests
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        // Get device ID from header (optional for web clients)
        var deviceId = context.Request.Headers["X-Device-Id"].FirstOrDefault();

        // Find active session - try with device ID first, then without
        UserSession? session = null;
        
        if (!string.IsNullOrEmpty(deviceId))
        {
            session = await dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId);
        }
        
        // If no session found with device ID, try to find any active session for the user
        if (session == null)
        {
            var now = DateTime.UtcNow;
            session = await dbContext.UserSessions
                .Where(s => s.UserId == userId && s.ExpiresAt > now)
                .OrderByDescending(s => s.LastActiveAt)
                .FirstOrDefaultAsync();
        }

        if (session != null)
        {
            // Check if session is expired
            if (session.IsExpired)
            {
                _logger.LogWarning("Session expired for user {UserId}, device {DeviceId}", userId, deviceId ?? "unknown");
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Session expired",
                    message = "Your session has expired. Please login again.",
                    sessionExpired = true
                });
                return;
            }

            // Update session activity and extend expiration (sliding expiration)
            var slidingExpiration = configuration.GetValue<bool>("Session:SlidingExpiration", true);
            var idleTimeoutMinutes = configuration.GetValue<int>("Session:IdleTimeoutMinutes", 30);
            var absoluteTimeoutMinutes = configuration.GetValue<int>("Session:AbsoluteTimeoutMinutes", 480);

            session.LastActiveAt = DateTime.UtcNow;

            if (slidingExpiration)
            {
                // Calculate new expiration time
                var newExpiresAt = DateTime.UtcNow.AddMinutes(idleTimeoutMinutes);
                
                // Don't extend beyond absolute timeout
                var absoluteExpiry = session.CreatedAt.AddMinutes(absoluteTimeoutMinutes);
                session.ExpiresAt = newExpiresAt < absoluteExpiry ? newExpiresAt : absoluteExpiry;
            }

            await dbContext.SaveChangesAsync();
        }
        else
        {
            // No session found - this is OK for web clients, JWT token validation is sufficient
            _logger.LogDebug("No session found for user {UserId}, continuing with JWT validation only", userId);
        }

        await _next(context);
    }
}

public static class SessionTimeoutMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionTimeout(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionTimeoutMiddleware>();
    }
}
