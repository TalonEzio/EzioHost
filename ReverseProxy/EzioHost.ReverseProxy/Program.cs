using EzioHost.Aspire.ServiceDefaults;
using EzioHost.ReverseProxy.Startup;

namespace EzioHost.ReverseProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.AddServiceDefaults();

            builder.ConfigureAuthentication();
            builder.ConfigureHttpClient();
            builder.ConfigureReverseProxy();

            builder.Services.AddCors(cfg =>
            {
                cfg.AddPolicy(nameof(EzioHost), policyBuilder =>
                {
                    policyBuilder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });

            var app = builder.Build();

            app.ConfigureMiddleware();

            app.Run();
        }

    }
}
