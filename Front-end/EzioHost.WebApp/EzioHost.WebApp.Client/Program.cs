using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

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
        builder.Services.AddHttpClient(nameof(EzioHost),
            cfg => { cfg.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); });


        await builder.Build().RunAsync();
    }
}