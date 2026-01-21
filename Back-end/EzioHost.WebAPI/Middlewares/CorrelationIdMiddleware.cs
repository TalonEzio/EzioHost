using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace EzioHost.WebAPI.Middlewares;

/// <summary>
/// Middleware to create and propagate correlation IDs across HTTP requests.
/// Uses Activity from System.Diagnostics for distributed tracing.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add to Activity for distributed tracing
        Activity.Current?.SetTag("correlation.id", correlationId);
        Activity.Current?.SetTag("http.request.id", correlationId);

        // Add to log scope
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Try to get correlation ID from request header
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationIdHeader))
        {
            return correlationIdHeader.ToString();
        }

        // Use Activity ID if available (from OpenTelemetry)
        if (Activity.Current?.Id != null)
        {
            return Activity.Current.Id;
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }
}
