using EzioHost.Core.Mappers;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Infrastructure.SqlServer.DataContext;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Infrastructure.SqlServer.UnitOfWorks;
using EzioHost.WebAPI.Hubs;
using EzioHost.WebAPI.Jobs;
using EzioHost.WebAPI.Middlewares;
using EzioHost.WebAPI.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Quartz;
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

            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
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
                            ValidAudience = jwtOidc.Audience,
                            ClockSkew = TimeSpan.Zero
                        };


                        x.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];

                                var path = context.HttpContext.Request.Path;
                                if (!string.IsNullOrEmpty(accessToken) &&
                                    path.StartsWithSegments("/hubs"))
                                {
                                    context.Token = accessToken;
                                }

                                return Task.CompletedTask;
                            }
                        };
                    }

                );

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
            builder.Services.AddScoped<ISettingProvider, SettingProvider>();

            builder.Services.AddScoped<IVideoService, VideoService>();

            builder.Services.AddScoped<IUserRepository, UserSqlServerRepository>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddScoped<IFileUploadRepository, FileUploadSqlServerRepository>();
            builder.Services.AddScoped<IFileUploadService, FileUploadService>();

            builder.Services.AddScoped<IProtectService, ProtectService>();

            builder.Services.AddScoped<IOnnxModelRepository, OnnxModelSqlServerRepository>();
            builder.Services.AddScoped<IOnnxModelService, OnnxModelService>();

            builder.Services.AddScoped<IUpscaleRepository, UpscaleSqlServerRepository>();
            builder.Services.AddScoped<IUpscaleService, UpscaleService>();


            builder.Services.AddQuartz(quartz =>
            {
                var videoProcessingJobKey = new JobKey(nameof(VideoProcessingJob));
                quartz.AddJob<VideoProcessingJob>(opts => opts.WithIdentity(videoProcessingJobKey).StoreDurably());

                quartz.AddTrigger(cfg => cfg
                    .WithIdentity(nameof(VideoProcessingJob))
                    .ForJob(videoProcessingJobKey)
                    .StartNow()
                    .WithSimpleSchedule(schedule => schedule
                        .WithIntervalInSeconds(10)
                        .RepeatForever()
                    )
                );

                var videoUpscaleJobKey = new JobKey(nameof(VideoUpscaleJob));
                quartz.AddJob<VideoUpscaleJob>(opts => opts.WithIdentity(videoUpscaleJobKey).StoreDurably());

                quartz.AddTrigger(cfg => cfg
                    .WithIdentity(nameof(VideoUpscaleJob))
                    .ForJob(videoUpscaleJobKey)
                    .StartNow()
                    .WithSimpleSchedule(schedule => schedule
                        .WithIntervalInSeconds(10)
                        .RepeatForever()
                    )
                );
            });

            builder.Services.AddQuartzHostedService(cfg =>
            {
                cfg.AwaitApplicationStarted = true;
                cfg.WaitForJobsToComplete = true;
            });

            builder.Services.AddAutoMapper(typeof(MapperClass));

            builder.Services.AddScoped<BindingUserIdMiddleware>();

            builder.Services.AddSignalR();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseExceptionHandler();
            }

            app.UseHttpsRedirection();

            var wwwrootDirectory = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            if (!Directory.Exists(wwwrootDirectory))
            {
                Directory.CreateDirectory(wwwrootDirectory);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = new FileExtensionContentTypeProvider
                {
                    Mappings = { [".m3u8"] = "application/x-mpegURL" }
                },
                FileProvider = new PhysicalFileProvider(wwwrootDirectory),
            });


            app.UseAuthentication();

            app.UseAuthorization();
            app.UseMiddleware<BindingUserIdMiddleware>();

            app.MapControllers();
            app.MapHub<VideoHub>("/hubs/VideoHub", cfg =>
            {
                cfg.LongPolling.PollTimeout = TimeSpan.FromSeconds(30);
                cfg.Transports = HttpTransportType.LongPolling | HttpTransportType.WebSockets;
            });

            await app.RunAsync();
        }
    }
}
