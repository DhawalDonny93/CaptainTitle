namespace AppBackendBasic.API.Middleware;

public class HttpsEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpsEnforcementMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public HttpsEnforcementMiddleware(
        RequestDelegate next,
        ILogger<HttpsEnforcementMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip HTTPS enforcement for CORS preflight requests (OPTIONS)
        if (context.Request.Method != "OPTIONS")
        {
            // Only enforce HTTPS in production
            if (!_environment.IsDevelopment())
            {
                if (!context.Request.IsHttps)
                {
                    var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
                    
                    _logger.LogWarning("HTTP request redirected to HTTPS: {Url}", context.Request.Path);
                    
                    context.Response.StatusCode = 301; // Permanent redirect
                    context.Response.Headers["Location"] = httpsUrl;
                    return;
                }

                // Add HSTS header (HTTP Strict Transport Security)
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            }

            // Add security headers
            AddSecurityHeaders(context);
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Referrer Policy
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Content Security Policy
        headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' https://api.openai.com; " +
            "frame-ancestors 'none';";

        // Permissions Policy (formerly Feature Policy)
        headers["Permissions-Policy"] = 
            "geolocation=(), " +
            "microphone=(), " +
            "camera=(), " +
            "payment=(), " +
            "usb=(), " +
            "magnetometer=(), " +
            "gyroscope=(), " +
            "accelerometer=()";

        // Remove server header for security
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
    }
}

public static class HttpsEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpsEnforcement(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpsEnforcementMiddleware>();
    }
}
