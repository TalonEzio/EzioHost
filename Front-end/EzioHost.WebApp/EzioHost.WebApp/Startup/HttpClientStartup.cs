using System.Text.Json.Serialization;
using EzioHost.WebApp.Client.Services;
using EzioHost.WebApp.Handlers;
using Refit;

namespace EzioHost.WebApp.Startup;

public static class HttpClientStartup
{
    public static WebApplicationBuilder ConfigureHttpClient(this WebApplicationBuilder builder, AppSettings settings)
    {
        builder.Services.AddTransient<RequestCookieHandler>();
        builder.Services.AddHttpContextAccessor();

        Action<IServiceProvider, HttpClient> configureClient = (_, client) =>
        {
            client.BaseAddress = new Uri(settings.Urls.ReverseProxy);
        };

        builder.Services.AddHttpClient(nameof(EzioHost), configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        var serializerOptions = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        var enumConverter =
            serializerOptions.Converters.SingleOrDefault(x => x.GetType() == typeof(JsonStringEnumConverter));
        if (enumConverter != null) serializerOptions.Converters.Remove(enumConverter);

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(serializerOptions)
        };

        builder.Services
            .AddRefitClient<IVideoApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<IOnnxModelApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<IUploadApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<IEncodingQualitySettingApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<IVideoSubtitleApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<ISubtitleTranscribeApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        builder.Services
            .AddRefitClient<ICloudflareStorageSettingApi>(refitSettings)
            .ConfigureHttpClient(configureClient)
            .AddHttpMessageHandler<RequestCookieHandler>();

        return builder;
    }
}