using EzioHost.Shared.Private.Endpoints;
using EzioHost.WebApp.Handlers;

namespace EzioHost.WebApp.Startup
{
    public static class HttpClientStartup
    {
        public static WebApplicationBuilder ConfigureHttpClient(this WebApplicationBuilder builder)
        {
            // Add HttpClient Factory
            builder.Services.AddTransient<RequestCookieHandler>();
            builder.Services.AddHttpClient(nameof(EzioHost), cfg =>
            {
                cfg.BaseAddress = new Uri(BaseUrl.ReverseProxyUrl);
            })
                .AddHttpMessageHandler<RequestCookieHandler>();

            builder.Services.AddHttpContextAccessor();

            return builder;
        }
    }
}

