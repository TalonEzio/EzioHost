using EzioHost.Aspire.ServiceDefaults;
using EzioHost.WebAPI.Startup;

namespace EzioHost.WebAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.ConfigureAppSettings(out var appSettings);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpContextAccessor();

        builder.ConfigureAuthentication(appSettings);
        builder.ConfigureDatabase(appSettings);
        builder.ConfigureServices();
        builder.ConfigureQuartz();
        builder.ConfigureAutoMapper();
        builder.ConfigureSignalR();

        var app = builder.Build();

        app.ConfigureStaticFiles();
        app.ConfigureMiddleware();

        await app.RunAsync();
    }
}