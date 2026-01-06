namespace EzioHost.ReverseProxy.Startup;

public static class HttpClientStartup
{
    public static WebApplicationBuilder ConfigureHttpClient(this WebApplicationBuilder builder, AppSettings settings)
    {
        builder.Services.AddHttpClient(nameof(EzioHost),
            cfg => { cfg.BaseAddress = new Uri(settings.Urls.WebApi); });

        return builder;
    }
}