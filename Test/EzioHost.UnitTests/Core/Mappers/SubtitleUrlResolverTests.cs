using EzioHost.Core.Mappers;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Mappers;

public class SubtitleUrlResolverTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly SubtitleUrlResolver _resolver;

    public SubtitleUrlResolverTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["ApiSettings:BaseUrl"])
            .Returns("/api");
        _resolver = new SubtitleUrlResolver(_configurationMock.Object);
    }

    [Fact]
    public void Resolve_ShouldBuildSubtitleUrl_WithSubtitleId()
    {
        // Arrange
        var subtitle = TestDataBuilder.CreateVideoSubtitle();
        var subtitleDto = new VideoSubtitleDto();

        // Act
        var result = _resolver.Resolve(subtitle, subtitleDto, null!, "dest", null!);

        // Assert
        result.Should().Contain("/api/VideoSubtitle/File/");
        result.Should().Contain(subtitle.Id.ToString());
    }

    [Fact]
    public void Resolve_ShouldNormalizeBaseUrl_WhenEndsWithSlash()
    {
        // Arrange
        _configurationMock.Setup(x => x["ApiSettings:BaseUrl"])
            .Returns("/api/");
        var resolver = new SubtitleUrlResolver(_configurationMock.Object);
        var subtitle = TestDataBuilder.CreateVideoSubtitle();

        // Act
        var result = resolver.Resolve(subtitle, new VideoSubtitleDto(), null!, "dest", null!);

        // Assert
        result.Should().NotContain("//");
    }
}
