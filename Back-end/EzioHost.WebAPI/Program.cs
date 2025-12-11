using EzioHost.Aspire.ServiceDefaults;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.WebAPI.Startup;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.WebAPI
{
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

            builder.ConfigureAuthentication(appSettings);
            builder.ConfigureDbContext();
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
}
