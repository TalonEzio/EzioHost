using EzioHost.Aspire.ServiceDefaults;
using EzioHost.WebAPI.Hubs;
using EzioHost.WebAPI.Middlewares;
using Microsoft.AspNetCore.Http.Connections;

namespace EzioHost.WebAPI.Startup;

public static class MiddlewareStartup
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        // Add correlation ID middleware first to ensure it's available for all subsequent middleware
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Add request logging middleware early in the pipeline
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Add global exception handler
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseExceptionHandler();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<VideoHub>("/hubs/VideoHub", cfg =>
        {
            cfg.LongPolling.PollTimeout = TimeSpan.FromSeconds(30);
            cfg.Transports = HttpTransportType.LongPolling | HttpTransportType.WebSockets;
        });

        return app;
    }
}