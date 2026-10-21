using System.Net;
using System.Text.Json;

namespace AppBackendBasic.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred.";

        // Customize status code and message based on exception type
        if (exception is UnauthorizedAccessException)
        {
            statusCode = HttpStatusCode.Unauthorized;
            message = "Unauthorized access.";
        }
        else if (exception is ArgumentException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = "Invalid request.";
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = _environment.IsDevelopment()
            ? new
            {
                StatusCode = context.Response.StatusCode,
                Message = exception.Message,
                StackTrace = exception.StackTrace ?? string.Empty,
                RequestId = context.TraceIdentifier
            }
            : new
            {
                StatusCode = context.Response.StatusCode,
                Message = message,
                StackTrace = string.Empty,
                RequestId = context.TraceIdentifier
            };

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}
