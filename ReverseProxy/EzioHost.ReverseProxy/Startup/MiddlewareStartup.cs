using EzioHost.Aspire.ServiceDefaults;

namespace EzioHost.ReverseProxy.Startup
{
    public static class MiddlewareStartup
    {
        public static WebApplication ConfigureMiddleware(this WebApplication app)
        {
            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseCors(nameof(EzioHost));

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapReverseProxy();

            return app;
        }
    }
}

