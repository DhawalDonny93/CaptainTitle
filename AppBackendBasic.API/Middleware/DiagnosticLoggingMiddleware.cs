namespace AppBackendBasic.API.Middleware;

public class DiagnosticLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DiagnosticLoggingMiddleware> _logger;

    public DiagnosticLoggingMiddleware(RequestDelegate next, ILogger<DiagnosticLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var path = context.Request.Path;
        var method = context.Request.Method;

        _logger.LogDebug("[{RequestId}] {Method} {Path}", requestId, method, path);

        try
        {
            await _next(context);

            _logger.LogDebug("[{RequestId}] {Method} {Path} → {StatusCode}",
                requestId, method, path, context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] {Method} {Path} failed: {Message}",
                requestId, method, path, ex.Message);
            throw;
        }
    }
}
