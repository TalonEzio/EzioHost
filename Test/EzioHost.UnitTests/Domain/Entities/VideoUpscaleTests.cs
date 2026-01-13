using EzioHost.Domain.Exceptions;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Entities;

public class VideoUpscaleTests
{
    [Fact]
    public void VideoUpscale_ShouldNormalizeOutputLocation_WhenSet()
    {
        // Arrange
        var upscale = TestDataBuilder.CreateVideoUpscale();
        var pathWithBackslashes = "upscaled\\test\\output.mp4";

        // Act
        upscale.OutputLocation = pathWithBackslashes;

        // Assert
        upscale.OutputLocation.Should().NotContain("\\");
    }

    [Fact]
    public void VideoUpscale_ShouldThrowException_WhenInvalidOutputLocation()
    {
        // Arrange
        var upscale = TestDataBuilder.CreateVideoUpscale();
        // Use a path with invalid characters that will cause UriFormatException
        var invalidPath = "http://[invalid-uri"; // Invalid URI format

        // Act
        var act = () => upscale.OutputLocation = invalidPath;

        // Assert
        act.Should().Throw<InvalidUriException>();
    }
}