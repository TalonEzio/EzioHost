using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class EncodingQualitySettingServiceTests
{
    private readonly Mock<IEncodingQualitySettingRepository> _repositoryMock;
    private readonly EncodingQualitySettingService _service;

    public EncodingQualitySettingServiceTests()
    {
        _repositoryMock = TestMockFactory.CreateEncodingQualitySettingRepositoryMock();
        _service = new EncodingQualitySettingService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetUserSettings_ShouldCreateDefaultSettings_WhenUserHasNoSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock.Setup(x => x.UserHasSettings(userId)).ReturnsAsync(false);
        _repositoryMock.Setup(x => x.GetSettingsByUserId(userId))
            .ReturnsAsync(new List<EncodingQualitySetting>());

        // Act
        var result = await _service.GetUserSettings(userId);

        // Assert
        result.Should().NotBeNull();
        _repositoryMock.Verify(x => x.UserHasSettings(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserSettings_ShouldReturnSettings_WhenUserHasSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = new List<EncodingQualitySetting>
        {
            new EncodingQualitySetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Resolution = VideoEnum.VideoResolution._720p,
                BitrateKbps = 3000,
                IsEnabled = true
            }
        };

        _repositoryMock.Setup(x => x.UserHasSettings(userId)).ReturnsAsync(true);
        _repositoryMock.Setup(x => x.GetSettingsByUserId(userId)).ReturnsAsync(settings);

        // Act
        var result = await _service.GetUserSettings(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Resolution.Should().Be(VideoEnum.VideoResolution._720p);
    }

    [Fact]
    public async Task GetActiveSettingsForEncoding_ShouldReturnOnlyEnabledSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activeSettings = new List<EncodingQualitySetting>
        {
            new EncodingQualitySetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Resolution = VideoEnum.VideoResolution._720p,
                BitrateKbps = 3000,
                IsEnabled = true
            }
        };

        _repositoryMock.Setup(x => x.UserHasSettings(userId)).ReturnsAsync(true);
        _repositoryMock.Setup(x => x.GetActiveSettingsByUserId(userId)).ReturnsAsync(activeSettings);

        // Act
        var result = await _service.GetActiveSettingsForEncoding(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].IsEnabled.Should().BeTrue();
    }
}
