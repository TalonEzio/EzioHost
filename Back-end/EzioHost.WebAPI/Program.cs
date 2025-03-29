using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Infrastructure.SqlServer.DataContext;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Infrastructure.SqlServer.UnitOfWorks;
using EzioHost.WebAPI.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EzioHost.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var appSettings = new AppSettings();

            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.Configuration.Bind(nameof(AppSettings), appSettings);

            builder.Services.AddControllers();

            builder.Services.AddOpenApi();

            builder.Services.AddProblemDetails();

            builder.Services.AddAuthentication(cfg =>
                {
                    cfg.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    cfg.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    var jwtOidc = appSettings.JwtOidc;

                    x.RequireHttpsMetadata = false;
                    x.Audience = jwtOidc.Audience;
                    x.MetadataAddress = jwtOidc.MetaDataAddress;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOidc.Issuer,
                        ValidAudience = jwtOidc.Audience
                    };
                });

            builder.Services.AddDbContext<EzioHostDbContext>(cfg =>
            {
                cfg.UseSqlServer(builder.Configuration.GetConnectionString(nameof(EzioHost)));
                cfg.EnableServiceProviderCaching();
            });

            builder.Services.AddScoped<IVideoRepository, VideoSqlServerRepository>();
            builder.Services.AddScoped<IVideoStreamRepository, VideoStreamSqlServerRepository>();

            builder.Services.AddScoped<IBaseUnitOfWork, BaseUnitOfWork>();
            builder.Services.AddScoped<IVideoUnitOfWork, VideoUnitOfWork>();

            builder.Services.AddScoped<IDirectoryProvider, DirectoryProvider>();

            builder.Services.AddScoped<IVideoService, VideoService>();

            builder.Services.AddScoped<IUserRepository, UserSqlServerRepository>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddScoped<IFileUploadRepository, FileUploadSqlServerRepository>();
            builder.Services.AddScoped<IFileUploadService, FileUploadService>();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseExceptionHandler();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllers();

#if SEED_DATA
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EzioHostDbContext>();
            await dbContext.SeedData();
#endif

            await app.RunAsync();
        }
    }
}
