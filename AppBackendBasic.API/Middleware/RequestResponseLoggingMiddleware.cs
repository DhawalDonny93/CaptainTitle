using System.Diagnostics;
using System.Text;

namespace AppBackendBasic.API.Middleware;

/// <summary>
/// Middleware to log all API requests and responses with timing information
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate unique request ID
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        // File / media streaming endpoints (video with HTTP Range, SRT, rendered MP4) must NOT be buffered into memory:
        // buffering would hold whole videos in RAM and breaks Range responses.
        if (IsStreamingPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Start timing
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        // Log request start
        await LogRequestAsync(context, requestId, startTime);

        // Capture original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware
            await _next(context);

            // Stop timing
            stopwatch.Stop();
            var endTime = DateTime.UtcNow;

            // Log response end
            await LogResponseAsync(context, requestId, startTime, endTime, stopwatch.ElapsedMilliseconds);

            // Copy response body back to original stream (rewind first: logging may have moved the position)
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var endTime = DateTime.UtcNow;

            _logger.LogError(ex, 
                "[{RequestId}] Request FAILED | Method: {Method} | Path: {Path} | Started: {StartTime} | Ended: {EndTime} | Duration: {Duration}ms | Error: {Error}",
                requestId,
                context.Request.Method,
                context.Request.Path,
                startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                endTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool IsStreamingPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/api/captions/media/", StringComparison.OrdinalIgnoreCase)
            || (value.StartsWith("/api/captions/projects/", StringComparison.OrdinalIgnoreCase)
                && (value.EndsWith("/download", StringComparison.OrdinalIgnoreCase) || value.EndsWith("/srt", StringComparison.OrdinalIgnoreCase)));
    }

    private async Task LogRequestAsync(HttpContext context, string requestId, DateTime startTime)
    {
        var request = context.Request;

        // Build request info
        var requestInfo = new StringBuilder();
        requestInfo.AppendLine($"Timestamp: {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC | Path: {request.Path}");

        _logger.LogInformation(requestInfo.ToString());
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, DateTime startTime, DateTime endTime, long durationMs)
    {
        var response = context.Response;

        // Build response info
        var responseInfo = new StringBuilder();
        responseInfo.AppendLine($"Started: {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC | Path: {context.Request.Path}");

        // Log response body (for small responses and non-binary content)
        if (response.Body.CanSeek && response.ContentType?.Contains("application/json") == true)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            if (bodyAsText.Length > 1000)
            {
                responseInfo.AppendLine($"Response Body: {bodyAsText.Substring(0, 1000)}... [TRUNCATED]");
            }
            else
            {
                responseInfo.AppendLine($"Response Body: {bodyAsText}");
            }
        }

        // Log with appropriate level based on status code
        if (response.StatusCode >= 500)
        {
            _logger.LogError(responseInfo.ToString());
        }
        else if (response.StatusCode >= 400)
        {
            _logger.LogWarning(responseInfo.ToString());
        }
        else
        {
            _logger.LogInformation(responseInfo.ToString());
        }
    }

}
