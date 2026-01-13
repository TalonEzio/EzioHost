using EzioHost.Core.Services.Implement;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class ProtectServiceTests
{
    private readonly ProtectService _protectService;

    public ProtectServiceTests()
    {
        _protectService = new ProtectService();
    }

    [Fact]
    public void GenerateRandomKey_ShouldReturnNonEmptyString()
    {
        // Act
        var result = _protectService.GenerateRandomKey();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRandomKey_ShouldReturnDifferentValues_OnMultipleCalls()
    {
        // Act
        var key1 = _protectService.GenerateRandomKey();
        var key2 = _protectService.GenerateRandomKey();

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GenerateRandomKey_ShouldReturnLowercaseHexString()
    {
        // Act
        var result = _protectService.GenerateRandomKey();

        // Assert
        result.Should().MatchRegex("^[0-9a-f]{16}$"); // 8 bytes = 16 hex chars
    }

    [Fact]
    public void GenerateRandomIv_ShouldReturnNonEmptyString()
    {
        // Act
        var result = _protectService.GenerateRandomIv();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRandomIv_ShouldReturnDifferentValues_OnMultipleCalls()
    {
        // Act
        var iv1 = _protectService.GenerateRandomIv();
        var iv2 = _protectService.GenerateRandomIv();

        // Assert
        iv1.Should().NotBe(iv2);
    }

    [Fact]
    public void GenerateRandomIv_ShouldReturnLowercaseHexString()
    {
        // Act
        var result = _protectService.GenerateRandomIv();

        // Assert
        result.Should().MatchRegex("^[0-9a-f]{32}$"); // 16 bytes = 32 hex chars
    }
}