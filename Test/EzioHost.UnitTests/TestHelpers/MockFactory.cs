using Moq;
using EzioHost.Core.Repositories;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Domain.Entities;
using System.Linq.Expressions;

namespace EzioHost.UnitTests.TestHelpers;

public static class TestMockFactory
{
    public static Mock<IVideoRepository> CreateVideoRepositoryMock()
    {
        return new Mock<IVideoRepository>();
    }

    public static Mock<IUserRepository> CreateUserRepositoryMock()
    {
        return new Mock<IUserRepository>();
    }

    public static Mock<IVideoStreamRepository> CreateVideoStreamRepositoryMock()
    {
        return new Mock<IVideoStreamRepository>();
    }

    public static Mock<IVideoSubtitleRepository> CreateVideoSubtitleRepositoryMock()
    {
        return new Mock<IVideoSubtitleRepository>();
    }

    public static Mock<IUpscaleRepository> CreateUpscaleRepositoryMock()
    {
        return new Mock<IUpscaleRepository>();
    }

    public static Mock<IOnnxModelRepository> CreateOnnxModelRepositoryMock()
    {
        return new Mock<IOnnxModelRepository>();
    }

    public static Mock<IFileUploadRepository> CreateFileUploadRepositoryMock()
    {
        return new Mock<IFileUploadRepository>();
    }

    public static Mock<IEncodingQualitySettingRepository> CreateEncodingQualitySettingRepositoryMock()
    {
        return new Mock<IEncodingQualitySettingRepository>();
    }

    public static Mock<IVideoUnitOfWork> CreateVideoUnitOfWorkMock()
    {
        return new Mock<IVideoUnitOfWork>();
    }

    public static Mock<IBaseUnitOfWork> CreateBaseUnitOfWorkMock()
    {
        return new Mock<IBaseUnitOfWork>();
    }

    public static Mock<IDirectoryProvider> CreateDirectoryProviderMock()
    {
        var mock = new Mock<IDirectoryProvider>();
        mock.Setup(x => x.GetWebRootPath()).Returns("wwwroot");
        mock.Setup(x => x.GetThumbnailFolder()).Returns("thumbnails");
        mock.Setup(x => x.GetBaseVideoFolder()).Returns("videos");
        return mock;
    }

    public static Mock<IStorageService> CreateStorageServiceMock()
    {
        return new Mock<IStorageService>();
    }

    public static Mock<ISettingProvider> CreateSettingProviderMock()
    {
        return new Mock<ISettingProvider>();
    }

    public static Mock<IProtectService> CreateProtectServiceMock()
    {
        return new Mock<IProtectService>();
    }

    public static Mock<IM3U8PlaylistService> CreateM3U8PlaylistServiceMock()
    {
        return new Mock<IM3U8PlaylistService>();
    }

    public static Mock<IVideoResolutionService> CreateVideoResolutionServiceMock()
    {
        return new Mock<IVideoResolutionService>();
    }

    public static Mock<IEncodingQualitySettingService> CreateEncodingQualitySettingServiceMock()
    {
        return new Mock<IEncodingQualitySettingService>();
    }

    public static Mock<IVideoService> CreateVideoServiceMock()
    {
        return new Mock<IVideoService>();
    }

    public static Mock<IUpscaleService> CreateUpscaleServiceMock()
    {
        return new Mock<IUpscaleService>();
    }

    public static Mock<IUserService> CreateUserServiceMock()
    {
        return new Mock<IUserService>();
    }

    public static Mock<IFileUploadService> CreateFileUploadServiceMock()
    {
        return new Mock<IFileUploadService>();
    }

    public static Mock<IVideoSubtitleService> CreateVideoSubtitleServiceMock()
    {
        return new Mock<IVideoSubtitleService>();
    }

    public static Mock<IOnnxModelService> CreateOnnxModelServiceMock()
    {
        return new Mock<IOnnxModelService>();
    }
}
