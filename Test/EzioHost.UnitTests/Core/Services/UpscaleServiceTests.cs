using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class UpscaleServiceTests
{
    private readonly Mock<IDirectoryProvider> _directoryProviderMock;
    private readonly Mock<ISettingProvider> _settingProviderMock;
    private readonly Mock<IUpscaleRepository> _upscaleRepositoryMock;
    private readonly Mock<IVideoService> _videoServiceMock;
    private readonly Mock<IVideoUnitOfWork> _videoUnitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IM3U8PlaylistService> _m3U8PlaylistServiceMock;
    private readonly Mock<ILogger<UpscaleService>> _loggerMock;
    private readonly Mock<IVideoRepository> _videoRepositoryMock;
    private readonly UpscaleService _service;

    public UpscaleServiceTests()
    {
        _directoryProviderMock = TestMockFactory.CreateDirectoryProviderMock();
        _settingProviderMock = TestMockFactory.CreateSettingProviderMock();
        _upscaleRepositoryMock = TestMockFactory.CreateUpscaleRepositoryMock();
        _videoServiceMock = new Mock<IVideoService>();
        _videoUnitOfWorkMock = TestMockFactory.CreateVideoUnitOfWorkMock();
        _mapperMock = new Mock<IMapper>();
        _m3U8PlaylistServiceMock = TestMockFactory.CreateM3U8PlaylistServiceMock();
        _loggerMock = new Mock<ILogger<UpscaleService>>();
        
        _videoRepositoryMock = TestMockFactory.CreateVideoRepositoryMock();
        _videoUnitOfWorkMock.Setup(x => x.VideoRepository).Returns(_videoRepositoryMock.Object);
        
        _service = new UpscaleService(
            _directoryProviderMock.Object,
            _settingProviderMock.Object,
            _upscaleRepositoryMock.Object,
            _videoServiceMock.Object,
            _videoUnitOfWorkMock.Object,
            _mapperMock.Object,
            _m3U8PlaylistServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AddNewVideoUpscale_ShouldAddToRepository()
    {
        // Arrange
        var upscale = TestDataBuilder.CreateVideoUpscale();
        _upscaleRepositoryMock
            .Setup(x => x.AddNewVideoUpscale(It.IsAny<VideoUpscale>()))
            .ReturnsAsync(upscale);

        // Act
        var result = await _service.AddNewVideoUpscale(upscale);

        // Assert
        result.Should().NotBeNull();
        _upscaleRepositoryMock.Verify(x => x.AddNewVideoUpscale(upscale), Times.Once);
    }

    [Fact]
    public async Task GetVideoUpscaleById_ShouldReturnUpscale_WhenExists()
    {
        // Arrange
        var upscaleId = Guid.NewGuid();
        var expectedUpscale = TestDataBuilder.CreateVideoUpscale(id: upscaleId);
        _upscaleRepositoryMock
            .Setup(x => x.GetVideoUpscaleById(upscaleId))
            .ReturnsAsync(expectedUpscale);

        // Act
        var result = await _service.GetVideoUpscaleById(upscaleId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(upscaleId);
        _upscaleRepositoryMock.Verify(x => x.GetVideoUpscaleById(upscaleId), Times.Once);
    }

    [Fact]
    public async Task GetVideoUpscaleById_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var upscaleId = Guid.NewGuid();
        _upscaleRepositoryMock
            .Setup(x => x.GetVideoUpscaleById(upscaleId))
            .ReturnsAsync((VideoUpscale?)null);

        // Act
        var result = await _service.GetVideoUpscaleById(upscaleId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateVideoUpscale_ShouldUpdateInRepository()
    {
        // Arrange
        var upscale = TestDataBuilder.CreateVideoUpscale();
        _upscaleRepositoryMock
            .Setup(x => x.UpdateVideoUpscale(It.IsAny<VideoUpscale>()))
            .ReturnsAsync(upscale);

        // Act
        var result = await _service.UpdateVideoUpscale(upscale);

        // Assert
        result.Should().NotBeNull();
        _upscaleRepositoryMock.Verify(x => x.UpdateVideoUpscale(upscale), Times.Once);
    }

    [Fact]
    public async Task DeleteVideoUpscale_ShouldDeleteFromRepository()
    {
        // Arrange
        var upscale = TestDataBuilder.CreateVideoUpscale();
        _upscaleRepositoryMock
            .Setup(x => x.DeleteVideoUpscale(It.IsAny<VideoUpscale>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteVideoUpscale(upscale);

        // Assert
        _upscaleRepositoryMock.Verify(x => x.DeleteVideoUpscale(upscale), Times.Once);
    }

    [Fact]
    public async Task GetVideoNeedUpscale_ShouldReturnUpscale_FromRepository()
    {
        // Arrange
        var expectedUpscale = TestDataBuilder.CreateVideoUpscale();
        _upscaleRepositoryMock
            .Setup(x => x.GetVideoNeedUpscale())
            .ReturnsAsync(expectedUpscale);

        // Act
        var result = await _service.GetVideoNeedUpscale();

        // Assert
        result.Should().NotBeNull();
        _upscaleRepositoryMock.Verify(x => x.GetVideoNeedUpscale(), Times.Once);
    }

    [Fact]
    public async Task GetVideoNeedUpscale_ShouldReturnNull_WhenNoUpscaleNeeded()
    {
        // Arrange
        _upscaleRepositoryMock
            .Setup(x => x.GetVideoNeedUpscale())
            .ReturnsAsync((VideoUpscale?)null);

        // Act
        var result = await _service.GetVideoNeedUpscale();

        // Assert
        result.Should().BeNull();
    }
}
