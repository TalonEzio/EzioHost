using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class M3U8PlaylistServiceTests
{
    private readonly M3U8PlaylistService _service;
    private readonly Mock<IVideoResolutionService> _videoResolutionServiceMock;

    public M3U8PlaylistServiceTests()
    {
        _videoResolutionServiceMock = TestMockFactory.CreateVideoResolutionServiceMock();
        _videoResolutionServiceMock.Setup(x => x.GetBandwidthForResolution(It.IsAny<string>()))
            .Returns(2800000);
        _videoResolutionServiceMock.Setup(x => x.GetResolutionDimensions(It.IsAny<string>()))
            .Returns("1280x720");

        _service = new M3U8PlaylistService(_videoResolutionServiceMock.Object);
    }

    [Fact]
    public async Task BuildFullPlaylistAsync_ShouldCreatePlaylistFile()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var stream1 = TestDataBuilder.CreateVideoStream(videoId: video.Id, resolution: VideoEnum.VideoResolution._720p);
        var stream2 =
            TestDataBuilder.CreateVideoStream(videoId: video.Id, resolution: VideoEnum.VideoResolution._1080p);
        video.VideoStreams = new List<VideoStream> { stream1, stream2 };

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "playlist.m3u8");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        try
        {
            // Act
            await _service.BuildFullPlaylistAsync(video, tempPath);

            // Assert
            File.Exists(tempPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempPath);
            content.Should().Contain("#EXTM3U");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            var dir = Path.GetDirectoryName(tempPath);
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir);
        }
    }

    [Fact]
    public async Task AppendStreamToPlaylistAsync_ShouldAppendToExistingFile()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var stream = TestDataBuilder.CreateVideoStream(videoId: video.Id, resolution: VideoEnum.VideoResolution._720p);

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "playlist.m3u8");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
        await File.WriteAllTextAsync(tempPath, "#EXTM3U\n");

        try
        {
            // Act
            await _service.AppendStreamToPlaylistAsync(video, stream, tempPath);

            // Assert
            var content = await File.ReadAllTextAsync(tempPath);
            content.Should().Contain("#EXTM3U");
            content.Should().Contain("#EXT-X-STREAM-INF:");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            var dir = Path.GetDirectoryName(tempPath);
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir);
        }
    }
}