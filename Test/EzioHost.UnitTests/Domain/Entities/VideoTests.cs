using EzioHost.Domain.Entities;
using EzioHost.Domain.Exceptions;
using EzioHost.Shared.Enums;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Entities;

public class VideoTests
{
    [Fact]
    public void Video_ShouldNormalizeRawLocation_WhenSet()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var pathWithBackslashes = "videos\\test\\video.mp4";

        // Act
        video.RawLocation = pathWithBackslashes;

        // Assert
        video.RawLocation.Should().NotContain("\\");
    }

    [Fact]
    public void Video_ShouldNormalizeM3U8Location_WhenSet()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var pathWithBackslashes = "videos\\test\\playlist.m3u8";

        // Act
        video.M3U8Location = pathWithBackslashes;

        // Assert
        video.M3U8Location.Should().NotContain("\\");
    }

    [Fact]
    public void Video_ShouldSetTitle_FromFileName()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var fileName = "my-video-file.mp4";

        // Act
        video.Title = fileName;

        // Assert
        video.Title.Should().Be("my-video-file");
    }

    [Fact]
    public void Video_ShouldUpdateResolution_WhenHeightIsSet()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();

        // Act
        video.Height = 720;

        // Assert
        video.Resolution.Should().Be(VideoEnum.VideoResolution._720p);
    }

    [Fact]
    public void Video_ShouldThrowException_WhenInvalidPath()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        // Use a path with invalid characters that will cause UriFormatException
        var invalidPath = "http://[invalid-uri"; // Invalid URI format

        // Act
        var act = () => video.RawLocation = invalidPath;

        // Assert
        act.Should().Throw<InvalidUriException>();
    }
}
