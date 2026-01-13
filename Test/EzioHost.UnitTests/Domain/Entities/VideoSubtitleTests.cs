using EzioHost.Domain.Entities;
using EzioHost.Domain.Exceptions;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Entities;

public class VideoSubtitleTests
{
    [Fact]
    public void VideoSubtitle_ShouldNormalizeLocalPath_WhenSet()
    {
        // Arrange
        var subtitle = TestDataBuilder.CreateVideoSubtitle();
        var pathWithBackslashes = "subtitles\\test\\subtitle.vtt";

        // Act
        subtitle.LocalPath = pathWithBackslashes;

        // Assert
        subtitle.LocalPath.Should().NotContain("\\");
    }

    [Fact]
    public void VideoSubtitle_ShouldThrowException_WhenInvalidLocalPath()
    {
        // Arrange
        var subtitle = TestDataBuilder.CreateVideoSubtitle();
        // Use a path with invalid characters that will cause UriFormatException
        var invalidPath = "http://[invalid-uri"; // Invalid URI format

        // Act
        var act = () => subtitle.LocalPath = invalidPath;

        // Assert
        act.Should().Throw<InvalidUriException>();
    }
}
