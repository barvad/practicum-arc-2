using System.Diagnostics;
using System.Text;

namespace CinemaAbyss.Proxy;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        // Enable buffering to read request body multiple times
        request.EnableBuffering();

        // Read request body
        var requestBody = await ReadRequestBodyAsync(request);

        // Log request information
        _logger.LogInformation("""
                               📥 Incoming Request:
                               Method: {Method}
                               Path: {Path}
                               QueryString: {QueryString}
                               Headers: {Headers}
                               Request Body: {RequestBody}
                               """,
            request.Method,
            request.Path,
            request.QueryString,
            FormatHeaders(request.Headers),
            requestBody);

        // Reset body stream position for downstream middleware
        request.Body.Position = 0;

        // Capture original response stream for logging
        var originalResponseBody = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Log response information
            var responseBodyContent = await ReadResponseBodyAsync(context.Response);
            await responseBody.CopyToAsync(originalResponseBody);

            _logger.LogInformation("""
                                   📤 Outgoing Response:
                                   Status Code: {StatusCode}
                                   Processing Time: {ElapsedMilliseconds} ms
                                   Response Body: {ResponseBody}
                                   """,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                responseBodyContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, """
                                 ❌ Request Processing Error:
                                 Method: {Method}
                                 Path: {Path}
                                 Error: {ErrorMessage}
                                 Stack Trace: {StackTrace}
                                 """,
                request.Method,
                request.Path,
                ex.Message,
                ex.StackTrace);

            // Restore original response stream for error handling
            context.Response.Body = originalResponseBody;
            throw;
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == 0 || !request.Body.CanRead)
            return "[No body or body not readable]";

        try
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8,
                leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return TruncateIfTooLong(body, 5000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read request body");
            return $"[Read error: {ex.Message}]";
        }
    }

    private async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        if (response.ContentLength == 0 || !response.Body.CanRead || !response.Body.CanSeek)
            return "[No body or body not readable]";

        try
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, Encoding.UTF8,
                leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return TruncateIfTooLong(body, 5000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read response body");
            return $"[Read error: {ex.Message}]";
        }
    }

    private string FormatHeaders(IHeaderDictionary headers)
    {
        var headerStrings = headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}");
        return string.Join("; ", headerStrings);
    }

    private string TruncateIfTooLong(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + $"... [truncated, total length: {text.Length}]";
    }
}