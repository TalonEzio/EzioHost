using EzioHost.Aspire.ServiceDefaults;
using EzioHost.ReverseProxy.Startup;

namespace EzioHost.ReverseProxy;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        var settings = new AppSettings();
        builder.Configuration.Bind(nameof(AppSettings), settings);

        builder.ConfigureAuthentication(settings);
        builder.ConfigureHttpClient(settings);
        builder.ConfigureReverseProxy();

        builder.Services.AddCors(cfg =>
        {
            cfg.AddPolicy(nameof(EzioHost),
                policyBuilder => { policyBuilder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); });
        });

        var app = builder.Build();

        app.ConfigureMiddleware();

        app.Run();
    }
}