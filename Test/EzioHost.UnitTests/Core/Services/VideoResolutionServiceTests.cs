using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Shared.Models;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class VideoResolutionServiceTests
{
    private readonly Mock<IEncodingQualitySettingService> _encodingQualitySettingServiceMock;
    private readonly VideoResolutionService _service;

    public VideoResolutionServiceTests()
    {
        _encodingQualitySettingServiceMock = TestMockFactory.CreateEncodingQualitySettingServiceMock();
        _service = new VideoResolutionService(_encodingQualitySettingServiceMock.Object);
    }

    [Theory]
    [InlineData("144p", 400000)]
    [InlineData("240p", 600000)]
    [InlineData("360p", 800000)]
    [InlineData("480p", 1400000)]
    [InlineData("720p", 2800000)]
    [InlineData("960p", 4000000)]
    [InlineData("1080p", 5000000)]
    [InlineData("1440p", 8000000)]
    [InlineData("1920p", 8000000)]
    [InlineData("2160p", 15000000)]
    [InlineData("AI Upscaled", 5000000)]
    [InlineData("unknown", 1000000)]
    public void GetBandwidthForResolution_ShouldReturnCorrectBandwidth(string resolution, int expectedBandwidth)
    {
        // Act
        var result = _service.GetBandwidthForResolution(resolution);

        // Assert
        result.Should().Be(expectedBandwidth);
    }

    [Theory]
    [InlineData("144p", "256x144")]
    [InlineData("240p", "426x240")]
    [InlineData("360p", "640x360")]
    [InlineData("480p", "854x480")]
    [InlineData("720p", "1280x720")]
    [InlineData("960p", "1280x960")]
    [InlineData("1080p", "1920x1080")]
    [InlineData("1440p", "2560x1440")]
    [InlineData("1920p", "2560x1920")]
    [InlineData("2160p", "3840x2160")]
    [InlineData("unknown", "1920x1080")]
    public void GetResolutionDimensions_ShouldReturnCorrectDimensions(string resolution, string expectedDimensions)
    {
        // Act
        var result = _service.GetResolutionDimensions(resolution);

        // Assert
        result.Should().Be(expectedDimensions);
    }

    [Fact]
    public async Task GetBandwidthForResolutionAsync_ShouldReturnDefault_WhenUserHasNoSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _encodingQualitySettingServiceMock
            .Setup(x => x.GetActiveSettingsForEncoding(userId))
            .ReturnsAsync(new List<EncodingQualitySettingDto>());

        // Act
        var result = await _service.GetBandwidthForResolutionAsync("720p", userId);

        // Assert
        result.Should().Be(2800000); // Default for 720p
    }
}