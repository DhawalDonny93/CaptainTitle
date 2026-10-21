using System.Text;

namespace AppBackendBasic.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    //private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        //_recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log the request
        await LogRequest(context);

        // Capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Continue down the middleware pipeline
            await _next(context);

            // Log the response
            await LogResponse(context, responseBody, originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequest(HttpContext context)
    {
        context.Request.EnableBuffering();

        //using var requestStream = _recyclableMemoryStreamManager.GetStream();
        using var requestStream = new MemoryStream();
        await context.Request.Body.CopyToAsync(requestStream);

        var body = ReadStreamInChunks(requestStream);
        context.Request.Body.Position = 0;

        // Don't log sensitive endpoints like login/register in production
        var path = context.Request.Path.Value?.ToLower();
        bool isSensitiveEndpoint = path?.Contains("login") == true || 
                                   path?.Contains("register") == true || 
                                   path?.Contains("password") == true;

        if (isSensitiveEndpoint)
        {
            _logger.LogInformation(
                "HTTP Request: {Method} {Scheme}://{Host}{Path}{QueryString}",
                context.Request.Method,
                context.Request.Scheme,
                context.Request.Host,
                context.Request.Path,
                context.Request.QueryString);
        }
        else
        {
            _logger.LogInformation(
                "HTTP Request: {Method} {Scheme}://{Host}{Path}{QueryString} Body: {Body}",
                context.Request.Method,
                context.Request.Scheme,
                context.Request.Host,
                context.Request.Path,
                context.Request.QueryString,
                body);
        }
    }

    private async Task LogResponse(HttpContext context, MemoryStream responseBody, Stream originalBodyStream)
    {
        responseBody.Position = 0;
        var body = ReadStreamInChunks(responseBody);
        responseBody.Position = 0;

        _logger.LogInformation(
            "HTTP Response: {StatusCode} Body: {Body}",
            context.Response.StatusCode,
            body);

        await responseBody.CopyToAsync(originalBodyStream);
    }

    private static string ReadStreamInChunks(MemoryStream stream)
    {
        const int readChunkBufferLength = 4096;
        stream.Seek(0, SeekOrigin.Begin);

        using var textWriter = new StringWriter();
        using var reader = new StreamReader(stream);

        var readChunk = new char[readChunkBufferLength];
        int readChunkLength;

        do
        {
            readChunkLength = reader.ReadBlock(readChunk, 0, readChunkBufferLength);
            textWriter.Write(readChunk, 0, readChunkLength);
        } while (readChunkLength > 0);

        return textWriter.ToString();
    }
}
