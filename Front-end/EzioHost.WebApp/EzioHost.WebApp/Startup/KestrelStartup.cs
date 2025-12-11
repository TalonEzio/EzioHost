using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace EzioHost.WebApp.Startup
{
    public static class KestrelStartup
    {
        public static WebApplicationBuilder ConfigureKestrel(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
                options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
            });

            return builder;
        }
    }
}

