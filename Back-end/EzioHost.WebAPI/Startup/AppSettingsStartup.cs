namespace EzioHost.WebAPI.Startup;

public static class AppSettingsStartup
{
    public static WebApplicationBuilder ConfigureAppSettings(this WebApplicationBuilder builder,
        out AppSettings appSettings)
    {
        appSettings = new AppSettings();
        builder.Configuration.Bind(nameof(AppSettings), appSettings);
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));

        return builder;
    }
}