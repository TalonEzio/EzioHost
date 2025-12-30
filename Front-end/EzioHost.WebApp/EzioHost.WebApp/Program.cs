using EzioHost.Aspire.ServiceDefaults;
using EzioHost.WebApp.Startup;

namespace EzioHost.WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.ConfigureRazorComponents();
        builder.ConfigureAuthentication();
        builder.ConfigureHttpClient();
        builder.ConfigureKestrel();
        builder.ConfigureAppSettings();
        builder.ConfigureSignalR();

        builder.Services.AddCors(cfg =>
        {
            cfg.AddPolicy(nameof(EzioHost), policyBuilder =>
            {
                policyBuilder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });


        var app = builder.Build();

        app.ConfigureMiddleware();

        app.Run();
    }
}