using EzioHost.Infrastructure.SqlServer.DataContexts;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.WebAPI.Startup
{
    public static class DbContextStartup
    {
        public static WebApplicationBuilder ConfigureDbContext(this WebApplicationBuilder builder)
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

