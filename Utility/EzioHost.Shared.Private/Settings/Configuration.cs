using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace EzioHost.Shared.Private.Settings;

public static class Configuration
{
    private static readonly Lazy<IConfigurationRoot> LazyRoot = new(() =>
    {
        var builder = new ConfigurationBuilder()
            .Add(new JsonConfigurationSource
            {
                Path = "sharedsettings.json",
                ReloadOnChange = true
            });
        return builder.Build();
    });

    public static IConfigurationRoot Root => LazyRoot.Value;
}