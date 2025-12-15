namespace EzioHost.WebApp.Startup;

public static class AppSettingsStartup
{
    public static WebApplicationBuilder ConfigureAppSettings(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration.GetSection(nameof(AppSettings)));

        return builder;
    }
}