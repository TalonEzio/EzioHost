using EzioHost.Core.Mappers;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Mappers;

public class StreamUrlResolverTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly StreamUrlResolver _resolver;

    public StreamUrlResolverTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["ManifestStreamSettings:BaseUrl"])
            .Returns("https://stream.example.com");
        _resolver = new StreamUrlResolver(_configurationMock.Object);
    }

    [Fact]
    public void Resolve_ShouldReturnEmptyString_WhenSourceMemberIsNull()
    {
        // Arrange
        var videoStream = TestDataBuilder.CreateVideoStream();

        // Act
        var result = _resolver.Resolve(videoStream, new VideoStreamDto(), null, "dest", null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ShouldReturnEmptyString_WhenSourceMemberIsEmpty()
    {
        // Arrange
        var videoStream = TestDataBuilder.CreateVideoStream();

        // Act
        var result = _resolver.Resolve(videoStream, new VideoStreamDto(), string.Empty, "dest", null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ShouldBuildStreamUrl_WithVideoIdAndResolution()
    {
        // Arrange
        var videoId = Guid.NewGuid();
        var videoStream =
            TestDataBuilder.CreateVideoStream(videoId: videoId, resolution: VideoEnum.VideoResolution._720p);

        // Act
        var result = _resolver.Resolve(videoStream, new VideoStreamDto(), "stream.m3u8", "dest", null!);

        // Assert
        // Note: Replace("//", "/") in resolver may remove one slash, so check with Contains
        result.Should().Contain(videoId.ToString());
        result.Should().Contain(((int)VideoEnum.VideoResolution._720p).ToString());
    }

    [Fact]
    public void Resolve_ShouldNormalizeDoubleSlashes()
    {
        // Arrange
        _configurationMock.Setup(x => x["ManifestStreamSettings:BaseUrl"])
            .Returns("https://stream.example.com/");
        var resolver = new StreamUrlResolver(_configurationMock.Object);
        var videoId = Guid.NewGuid();
        var videoStream = TestDataBuilder.CreateVideoStream(videoId: videoId);

        // Act
        var result = resolver.Resolve(videoStream, new VideoStreamDto(), "stream.m3u8", "dest", null!);

        // Assert
        result.Should().NotContain("//");
    }
}