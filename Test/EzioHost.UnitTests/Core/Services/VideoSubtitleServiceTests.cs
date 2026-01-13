using System.Text;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class VideoSubtitleServiceTests
{
    private readonly Mock<IDirectoryProvider> _directoryProviderMock;
    private readonly VideoSubtitleService _service;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IVideoRepository> _videoRepositoryMock;
    private readonly Mock<IVideoSubtitleRepository> _videoSubtitleRepositoryMock;

    public VideoSubtitleServiceTests()
    {
        _videoSubtitleRepositoryMock = TestMockFactory.CreateVideoSubtitleRepositoryMock();
        _videoRepositoryMock = new Mock<IVideoRepository>();
        _directoryProviderMock = TestMockFactory.CreateDirectoryProviderMock();
        _storageServiceMock = TestMockFactory.CreateStorageServiceMock();

        _directoryProviderMock.Setup(x => x.GetWebRootPath()).Returns("wwwroot");
        _directoryProviderMock.Setup(x => x.GetBaseVideoFolder()).Returns("videos");

        _service = new VideoSubtitleService(
            _videoSubtitleRepositoryMock.Object,
            _videoRepositoryMock.Object,
            _directoryProviderMock.Object,
            _storageServiceMock.Object);
    }

    [Fact]
    public async Task GetSubtitlesByVideoIdAsync_ShouldReturnSubtitles()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var subtitles = new List<VideoSubtitle>
        {
            TestDataBuilder.CreateVideoSubtitle(videoId: videoId, language: "en"),
            TestDataBuilder.CreateVideoSubtitle(videoId: videoId, language: "vi")
        };

        _videoSubtitleRepositoryMock
            .Setup(x => x.GetSubtitlesByVideoId(videoId))
            .ReturnsAsync(subtitles);

        // Act
        var result = await _service.GetSubtitlesByVideoIdAsync(videoId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _videoSubtitleRepositoryMock.Verify(x => x.GetSubtitlesByVideoId(videoId), Times.Once);
    }

    [Fact]
    public async Task GetSubtitleByIdAsync_ShouldReturnSubtitle_WhenExists()
    {
        // Arrange
        var subtitleId = Guid.NewGuid();
        var expectedSubtitle = TestDataBuilder.CreateVideoSubtitle(subtitleId);

        _videoSubtitleRepositoryMock
            .Setup(x => x.GetSubtitleById(subtitleId))
            .ReturnsAsync(expectedSubtitle);

        // Act
        var result = await _service.GetSubtitleByIdAsync(subtitleId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedSubtitle);
        _videoSubtitleRepositoryMock.Verify(x => x.GetSubtitleById(subtitleId), Times.Once);
    }

    [Fact]
    public async Task GetSubtitleByIdAsync_ShouldReturnNull_WhenDoesNotExist()
    {
        // Arrange
        var subtitleId = Guid.NewGuid();

        _videoSubtitleRepositoryMock
            .Setup(x => x.GetSubtitleById(subtitleId))
            .ReturnsAsync((VideoSubtitle?)null);

        // Act
        var result = await _service.GetSubtitleByIdAsync(subtitleId);

        // Assert
        result.Should().BeNull();
        _videoSubtitleRepositoryMock.Verify(x => x.GetSubtitleById(subtitleId), Times.Once);
    }

    [Fact]
    public async Task UploadSubtitleAsync_ShouldThrowArgumentException_WhenFileExtensionIsInvalid()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var video = TestDataBuilder.CreateVideo(videoId);
        var invalidFileStream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));

        _videoRepositoryMock
            .Setup(x => x.GetVideoById(videoId))
            .ReturnsAsync(video);

        // Act
        var act = async () => await _service.UploadSubtitleAsync(
            videoId, "en", invalidFileStream, "test.txt", 100, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Chỉ chấp nhận file .vtt*");
    }

    [Fact]
    public async Task UploadSubtitleAsync_ShouldThrowArgumentException_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var video = TestDataBuilder.CreateVideo(videoId);
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("WEBVTT\n\n00:00:00.000 --> 00:00:01.000\nTest"));
        var largeFileSize = 6 * 1024 * 1024; // 6MB > 5MB limit

        _videoRepositoryMock
            .Setup(x => x.GetVideoById(videoId))
            .ReturnsAsync(video);

        // Act
        var act = async () => await _service.UploadSubtitleAsync(
            videoId, "en", fileStream, "test.vtt", largeFileSize, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Kích thước file không được vượt quá*");
    }

    [Fact]
    public async Task UploadSubtitleAsync_ShouldThrowArgumentException_WhenLanguageIsInvalid()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var video = TestDataBuilder.CreateVideo(videoId);
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("WEBVTT\n\n00:00:00.000 --> 00:00:01.000\nTest"));

        _videoRepositoryMock
            .Setup(x => x.GetVideoById(videoId))
            .ReturnsAsync(video);

        // Act
        var act = async () => await _service.UploadSubtitleAsync(
            videoId, "en@#$", fileStream, "test.vtt", 100, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Tên ngôn ngữ chỉ được chứa*");
    }

    [Fact]
    public async Task UploadSubtitleAsync_ShouldThrowArgumentException_WhenVideoDoesNotExist()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("WEBVTT\n\n00:00:00.000 --> 00:00:01.000\nTest"));

        _videoRepositoryMock
            .Setup(x => x.GetVideoById(videoId))
            .ReturnsAsync((Video?)null);

        // Act
        var act = async () => await _service.UploadSubtitleAsync(
            videoId, "en", fileStream, "test.vtt", 100, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Video không tồn tại*");
    }

    [Fact]
    public async Task DeleteSubtitleAsync_ShouldThrowArgumentException_WhenSubtitleDoesNotExist()
    {
        // Arrange
        var subtitleId = Guid.NewGuid();

        _videoSubtitleRepositoryMock
            .Setup(x => x.GetSubtitleById(subtitleId))
            .ReturnsAsync((VideoSubtitle?)null);

        // Act
        var act = async () => await _service.DeleteSubtitleAsync(subtitleId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Subtitle không tồn tại*");
    }

    [Fact]
    public async Task GetSubtitleFileStreamAsync_ShouldReturnStream_WhenFileExists()
    {
        // Arrange
        var subtitleId = Guid.NewGuid();

        // Create a temporary file for testing FIRST
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Create the file with a simple path that will match after normalization
        var expectedLocalPath = "test.vtt"; // Simple path that won't be changed by normalization
        var tempPath = Path.Combine(tempDir, expectedLocalPath);
        await File.WriteAllTextAsync(tempPath, "WEBVTT\n\n00:00:00.000 --> 00:00:01.000\nTest");

        // Setup mock BEFORE creating service
        var directoryProviderMock = new Mock<IDirectoryProvider>();
        directoryProviderMock.Setup(x => x.GetWebRootPath()).Returns(tempDir);
        directoryProviderMock.Setup(x => x.GetBaseVideoFolder()).Returns(tempDir);

        // Create a new service with the mocked directory provider
        var service = new VideoSubtitleService(
            _videoSubtitleRepositoryMock.Object,
            _videoRepositoryMock.Object,
            directoryProviderMock.Object,
            _storageServiceMock.Object);

        var subtitle = TestDataBuilder.CreateVideoSubtitle(subtitleId);
        subtitle.LocalPath = expectedLocalPath; // Set to match file path
        _videoSubtitleRepositoryMock
            .Setup(x => x.GetSubtitleById(subtitleId))
            .ReturnsAsync(subtitle);

        try
        {
            // Act
            var result = await service.GetSubtitleFileStreamAsync(subtitleId);

            // Assert
            result.Should().NotBeNull();
            result.CanRead.Should().BeTrue();
            result.Dispose();
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task GetSubtitleFileStreamAsync_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var subtitleId = Guid.NewGuid();
        var subtitle = TestDataBuilder.CreateVideoSubtitle(subtitleId);
        subtitle.LocalPath = "subtitles/nonexistent.vtt";
        _videoSubtitleRepositoryMock
            .Setup(x => x.GetSubtitleById(subtitleId))
            .ReturnsAsync(subtitle);

        // Act
        var act = async () => await _service.GetSubtitleFileStreamAsync(subtitleId);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*File subtitle không tồn tại*");
    }
}