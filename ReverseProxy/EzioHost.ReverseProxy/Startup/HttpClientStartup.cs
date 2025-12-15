using EzioHost.Shared.Private.Endpoints;

namespace EzioHost.ReverseProxy.Startup;

public static class HttpClientStartup
{
    public static WebApplicationBuilder ConfigureHttpClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient(nameof(EzioHost),
            cfg => { cfg.BaseAddress = new Uri(BaseUrl.WebApiUrl); });

        return builder;
    }
}