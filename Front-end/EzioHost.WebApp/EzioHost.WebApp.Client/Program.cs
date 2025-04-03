using EzioHost.Shared.Common;
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

        builder.Services.AddHttpClient(nameof(EzioHost), cfg =>
        {
            cfg.BaseAddress = new Uri(BaseUrlConstants.ReverseProxyUrl);
        });
        builder.Services.AddScoped(provider => provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(EzioHost)));


        await builder.Build().RunAsync();
    }
}
