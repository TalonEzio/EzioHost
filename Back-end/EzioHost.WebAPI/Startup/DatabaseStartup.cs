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
            builder.Services.AddDbContext<EzioHostDbContext>(cfg =>
            {
                cfg.UseSqlServer(builder.Configuration.GetConnectionString(nameof(EzioHost)));
                cfg.EnableServiceProviderCaching();
            });
            return builder;
        }
    }
}