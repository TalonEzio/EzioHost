using EzioHost.Domain.Entities;
using EzioHost.Domain.Exceptions;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Entities;

public class VideoStreamTests
{
    [Fact]
    public void VideoStream_ShouldNormalizeM3U8Location_WhenSet()
    {
        // Arrange
        var stream = TestDataBuilder.CreateVideoStream();
        var pathWithBackslashes = "streams\\test\\playlist.m3u8";

        // Act
        stream.M3U8Location = pathWithBackslashes;

        // Assert
        stream.M3U8Location.Should().NotContain("\\");
    }

    [Fact]
    public void VideoStream_ShouldThrowException_WhenInvalidM3U8Location()
    {
        // Arrange
        var stream = TestDataBuilder.CreateVideoStream();
        // Use a path with invalid characters that will cause UriFormatException
        var invalidPath = "http://[invalid-uri"; // Invalid URI format

        // Act
        var act = () => stream.M3U8Location = invalidPath;

        // Assert
        act.Should().Throw<InvalidUriException>();
    }
}
