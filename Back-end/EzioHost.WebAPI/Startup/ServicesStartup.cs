using Amazon.S3;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Infrastructure.SqlServer.UnitOfWorks;
using EzioHost.Infrastructure.Storage.CloudFlare.Services.Implement;
using EzioHost.WebAPI.Providers;
using Microsoft.Extensions.Options;

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

        builder.Services.AddScoped<IEncodingQualitySettingRepository, EncodingQualitySettingSqlServerRepository>();
        builder.Services.AddScoped<IEncodingQualitySettingService, EncodingQualitySettingService>();

        builder.Services.AddScoped<IStorageService, R2StorageService>();

        builder.Services.AddScoped<IAmazonS3>(serviceProvider =>
        {
            var appSettings = serviceProvider.GetService<IOptionsMonitor<AppSettings>>();

            var storageSettings = appSettings!.CurrentValue.Storage;

            var s3Config = new AmazonS3Config
            {
                ServiceURL = storageSettings.ServiceUrl,
                ForcePathStyle = true
            };
            var s3 = new AmazonS3Client(storageSettings.AccessKey, storageSettings.SecretKey, s3Config);
            return s3;
        });
        return builder;
    }
}