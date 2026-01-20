using EzioHost.Infrastructure.SqlServer.DataContexts;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.WebAPI.Startup;

public static class DatabaseStartup
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder ConfigureDatabase(AppSettings appSettings)
        {
            return builder.ConfigureEfCore(appSettings);
        }

        private WebApplicationBuilder ConfigureEfCore(AppSettings appSettings)
        {
            // Skip database configuration in Testing environment
            // TestWebApplicationFactory will configure in-memory database instead
            if (builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
                return builder;

            builder.Services.AddDbContext<EzioHostDbContext>(cfg =>
            {
                cfg.UseSqlServer(builder.Configuration.GetConnectionString(nameof(EzioHost)));
                cfg.EnableServiceProviderCaching();
                cfg.LogTo(_ => { }, Microsoft.Extensions.Logging.LogLevel.None);
            });
            return builder;
        }
    }
}