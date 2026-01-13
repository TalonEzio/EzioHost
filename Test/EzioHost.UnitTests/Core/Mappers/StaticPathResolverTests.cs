using EzioHost.Core.Mappers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Mappers;

public class StaticPathResolverTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly StaticPathResolver _resolver;

    public StaticPathResolverTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["StaticFileSettings:WebApiPrefixStaticFile"])
            .Returns("/static");
        _resolver = new StaticPathResolver(_configurationMock.Object);
    }

    [Fact]
    public void Resolve_ShouldReturnEmptyString_WhenSourceMemberIsNull()
    {
        // Act
        var result = _resolver.Resolve(new object(), new object(), null, "dest", null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ShouldReturnEmptyString_WhenSourceMemberIsEmpty()
    {
        // Act
        var result = _resolver.Resolve(new object(), new object(), string.Empty, "dest", null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ShouldBuildStaticPath_WithPrefix()
    {
        // Arrange
        var path = "images/test.jpg";

        // Act
        var result = _resolver.Resolve(new object(), new object(), path, "dest", null!);

        // Assert
        result.Should().Contain("/static");
        result.Should().Contain("test.jpg");
    }

    [Fact]
    public void Resolve_ShouldNormalizeSlashes()
    {
        // Arrange
        var path = "//images//test.jpg";

        // Act
        var result = _resolver.Resolve(new object(), new object(), path, "dest", null!);

        // Assert
        result.Should().NotContain("//");
    }
}
