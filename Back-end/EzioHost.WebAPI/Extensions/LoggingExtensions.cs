using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EzioHost.WebAPI.Extensions;

/// <summary>
/// Extension methods for standardized logging patterns across the application.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Gets the correlation ID from the current HTTP context or Activity.
    /// </summary>
    public static string GetCorrelationId(this HttpContext? context)
    {
        if (context != null && context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.ToString();
        }

        return Activity.Current?.Id ?? "unknown";
    }

    /// <summary>
    /// Gets the user ID from the current HTTP context.
    /// </summary>
    public static string GetUserId(this HttpContext? context)
    {
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst("sub")?.Value ?? "unknown";
        }

        return "anonymous";
    }

    /// <summary>
    /// Creates a log scope with correlation ID and user ID.
    /// </summary>
    public static IDisposable BeginScopeWithContext(this ILogger logger, HttpContext? context)
    {
        var correlationId = context.GetCorrelationId();
        var userId = context.GetUserId();

        return logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId
        });
    }

    /// <summary>
    /// Logs a database operation with standardized format.
    /// </summary>
    public static void LogDatabaseOperation(
        this ILogger logger,
        string operation,
        string entityType,
        object? entityId = null,
        LogLevel level = LogLevel.Information)
    {
        var message = "Database {Operation} on {EntityType}";
        var args = new object[] { operation, entityType };

        if (entityId != null)
        {
            message += " with Id {EntityId}";
            args = new object[] { operation, entityType, entityId };
        }

        logger.Log(level, message, args);
    }

    /// <summary>
    /// Logs a file operation with standardized format.
    /// </summary>
    public static void LogFileOperation(
        this ILogger logger,
        string operation,
        string filePath,
        long? fileSize = null,
        LogLevel level = LogLevel.Information)
    {
        // Sanitize file path - only log relative path or filename
        var sanitizedPath = SanitizeFilePath(filePath);

        if (fileSize.HasValue)
        {
            logger.Log(level,
                "File {Operation} on {FilePath} (Size: {FileSize} bytes)",
                operation,
                sanitizedPath,
                fileSize.Value);
        }
        else
        {
            logger.Log(level,
                "File {Operation} on {FilePath}",
                operation,
                sanitizedPath);
        }
    }

    /// <summary>
    /// Logs an external service call with timing information.
    /// </summary>
    public static void LogExternalServiceCall(
        this ILogger logger,
        string serviceName,
        string operation,
        TimeSpan duration,
        bool success = true,
        Exception? exception = null)
    {
        if (success)
        {
            logger.LogInformation(
                "External service {ServiceName} operation {Operation} completed in {DurationMs}ms",
                serviceName,
                operation,
                duration.TotalMilliseconds);
        }
        else
        {
            logger.LogError(exception,
                "External service {ServiceName} operation {Operation} failed after {DurationMs}ms",
                serviceName,
                operation,
                duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Logs a long-running operation with progress information.
    /// </summary>
    public static void LogOperationProgress(
        this ILogger logger,
        string operationName,
        int current,
        int total,
        object? contextId = null)
    {
        var percentage = total > 0 ? (current * 100.0 / total) : 0;
        var message = "Operation {OperationName} progress: {Current}/{Total} ({Percentage:F1}%)";

        if (contextId != null)
        {
            message += " - Context: {ContextId}";
            logger.LogInformation(message, operationName, current, total, percentage, contextId);
        }
        else
        {
            logger.LogInformation(message, operationName, current, total, percentage);
        }
    }

    /// <summary>
    /// Sanitizes file paths to avoid logging sensitive information.
    /// </summary>
    private static string SanitizeFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "unknown";

        // If it's already a relative path, return as is
        if (!Path.IsPathRooted(filePath))
            return filePath;

        // Extract just the filename and parent directory
        try
        {
            var fileName = Path.GetFileName(filePath);
            var directory = Path.GetDirectoryName(filePath);
            var parentDir = directory != null ? Path.GetFileName(directory) : "unknown";

            return $"{parentDir}/{fileName}";
        }
        catch
        {
            // If path parsing fails, return a safe placeholder
            return "sanitized-path";
        }
    }
}
