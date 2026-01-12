using System.Text.Json.Serialization;
using EzioHost.WebApp.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;

namespace EzioHost.WebApp.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services
            .AddCascadingAuthenticationState()
            .AddAuthenticationStateDeserialization();

        builder.Services.AddAuthorizationCore();

        // Use BaseAddress from HostEnvironment (automatically gets from browser location.origin)
        // This works with reverse proxy - the client will use the same origin it's loaded from
        var baseAddress = builder.HostEnvironment.BaseAddress;
        builder.Services.AddHttpClient(nameof(EzioHost),
            cfg => { cfg.BaseAddress = new Uri(baseAddress); });

        // Register Refit APIs

        var serializer = SystemTextJsonContentSerializer.GetDefaultJsonSerializerOptions();

        serializer.Converters.Remove(serializer.Converters.Single(x => x.GetType() == typeof(JsonStringEnumConverter)));
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(serializer)
        };
        builder.Services
            .AddRefitClient<IVideoApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));

        builder.Services
            .AddRefitClient<IOnnxModelApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));

        builder.Services
            .AddRefitClient<IUploadApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));

        builder.Services
            .AddRefitClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));

        builder.Services
            .AddRefitClient<IEncodingQualitySettingApi>(refitSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));

        await builder.Build().RunAsync();
    }
}