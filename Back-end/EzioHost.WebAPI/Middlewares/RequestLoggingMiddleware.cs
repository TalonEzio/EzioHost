using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace EzioHost.WebAPI.Middlewares;

/// <summary>
/// Middleware to log HTTP requests and responses with timing information.
/// Skips logging for health checks and static files.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private static readonly string[] SkipPaths = { "/health", "/alive", "/openapi", "/swagger" };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks and static files
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? Activity.Current?.Id 
                           ?? "unknown";

        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value ?? "unknown"
            : "anonymous";

        // Log request start
        _logger.LogInformation(
            "HTTP {Method} {Path} started. UserId: {UserId}, CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            userId,
            correlationId);

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Log request completion
            _logger.LogInformation(
                "HTTP {Method} {Path} completed with status {StatusCode} in {ElapsedMilliseconds}ms. UserId: {UserId}, CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                userId,
                correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "HTTP {Method} {Path} failed with exception after {ElapsedMilliseconds}ms. UserId: {UserId}, CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                userId,
                correlationId);
            
            throw;
        }
    }

    private static bool ShouldSkipLogging(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        return SkipPaths.Any(skipPath => pathValue.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase)) ||
               pathValue.StartsWith("/static", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/_", StringComparison.OrdinalIgnoreCase);
    }
}
