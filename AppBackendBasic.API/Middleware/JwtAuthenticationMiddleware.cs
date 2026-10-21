using System.Net;
using System.Security.Claims;
using AppBackendBasic.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace AppBackendBasic.API.Middleware
{
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        // Paths that don't require authentication
        private static readonly HashSet<string> _publicPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/google",
            "/api/auth/apple",
            "/api/auth/mobile-login/send-otp",
            "/api/auth/mobile-login/verify-otp",
            "/api/auth/refresh-token",
            "/api/auth/google/callback",
            "/api/auth/apple/callback",
            "/api/auth/oauth/initiate",
            "/api/auth/email-login/send-otp",
            "/api/auth/email-login/verify",
            "/api/auth/email-login/resend",
            "/api/auth/request-password-reset",
            "/api/auth/reset-password",
            "/api/webhooks",
            "/health",
            "/swagger"
        };

        public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Skip authentication for public paths
            if (IsPublicPath(path))
            {
                await _next(context);
                return;
            }

            // Check for Authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Missing or invalid Authorization header for path: {Path}", path);
                await SendUnauthorizedResponse(context, "Unauthorized. Missing or invalid token.");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                // Use TokenService to validate the token
                if (!tokenService.ValidateToken(token))
                {
                    _logger.LogWarning("Invalid token for path: {Path}", path);
                    await SendUnauthorizedResponse(context, "Unauthorized. Invalid token.");
                    return;
                }

                // Get claims from token
                var principal = tokenService.GetPrincipalFromExpiredToken(token);
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Token missing user ID claim for path: {Path}", path);
                    await SendUnauthorizedResponse(context, "Unauthorized. Invalid token.");
                    return;
                }

                // Set the user principal for the current request
                context.User = principal;

                await _next(context);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Expired token for path: {Path}", path);
                await SendUnauthorizedResponse(context, "Unauthorized. Token has expired.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token for path: {Path}", path);
                await SendUnauthorizedResponse(context, "Unauthorized. Invalid token.");
            }
        }

        private static async Task SendUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message });
        }

        private static bool IsPublicPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check if path starts with any of the public paths
            foreach (var publicPath in _publicPaths)
            {
                if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    // Extension method for adding the middleware to the pipeline
    public static class JwtAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtAuthenticationMiddleware>();
        }
    }
}
