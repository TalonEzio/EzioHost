using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Middlewares;

/// <summary>
/// Global exception handler middleware to catch and log unhandled exceptions.
/// Returns sanitized error messages to clients.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? Activity.Current?.Id 
                           ?? "unknown";

        var userId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("sub")?.Value ?? "unknown"
            : "anonymous";

        // Log the exception with full context
        _logger.LogError(exception,
            "Unhandled exception occurred. Method: {Method}, Path: {Path}, UserId: {UserId}, CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            userId,
            correlationId);

        // Create problem details response
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occurred while processing your request.",
            Status = (int)HttpStatusCode.InternalServerError,
            Instance = context.Request.Path
        };

        // Add correlation ID to response
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id;

        // Include exception details in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Detail = exception.ToString();
            problemDetails.Extensions["exception"] = new
            {
                message = exception.Message,
                type = exception.GetType().Name,
                stackTrace = exception.StackTrace
            };
        }
        else
        {
            problemDetails.Detail = "An internal server error occurred. Please contact support with the correlation ID.";
        }

        context.Response.StatusCode = problemDetails.Status.Value;
        context.Response.ContentType = "application/problem+json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, jsonOptions));
    }
}
