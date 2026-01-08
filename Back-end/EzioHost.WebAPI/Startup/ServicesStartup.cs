using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Infrastructure.SqlServer.UnitOfWorks;
using EzioHost.WebAPI.Providers;

namespace EzioHost.WebAPI.Startup;

public static class ServicesStartup
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
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

        builder.Services.AddScoped<IM3U8PlaylistService, M3U8PlaylistService>();
        builder.Services.AddScoped<IVideoResolutionService, VideoResolutionService>();

        return builder;
    }
}