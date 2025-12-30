using System.Text.Json.Serialization;
using EzioHost.Shared.Private.Endpoints;
using EzioHost.WebApp.Client.Services;
using EzioHost.WebApp.Handlers;
using Refit;

namespace EzioHost.WebApp.Startup;

public static class HttpClientStartup
{
    public static WebApplicationBuilder ConfigureHttpClient(this WebApplicationBuilder builder)
    {
        // Add HttpClient Factory
        builder.Services.AddTransient<RequestCookieHandler>();
        builder.Services.AddHttpClient(nameof(EzioHost), cfg => { cfg.BaseAddress = new Uri(BaseUrl.ReverseProxyUrl); })
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services.AddHttpContextAccessor();

        // Register Refit APIs
        var baseAddress = new Uri(BaseUrl.ReverseProxyUrl);
        builder.Services
            .AddRefitClient<IVideoApi>()
            .ConfigureHttpClient(c => c.BaseAddress = baseAddress)
            .AddHttpMessageHandler<RequestCookieHandler>()
            ;

        var serializer = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        serializer.Converters.Remove(serializer.Converters.Single(x => x.GetType() == typeof(JsonStringEnumConverter)));
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(serializer)
        };

        builder.Services
            .AddRefitClient<IOnnxModelApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = baseAddress)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<IUploadApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = baseAddress)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = baseAddress)
            .AddHttpMessageHandler<RequestCookieHandler>();

        return builder;
    }
}