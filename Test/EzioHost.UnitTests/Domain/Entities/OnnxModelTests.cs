using EzioHost.Domain.Entities;
using EzioHost.Domain.Exceptions;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Entities;

public class OnnxModelTests
{
    [Fact]
    public void OnnxModel_ShouldNormalizeDemoInput_WhenSet()
    {
        // Arrange
        var model = TestDataBuilder.CreateOnnxModel();
        var pathWithBackslashes = "demo\\input\\test.jpg";

        // Act
        model.DemoInput = pathWithBackslashes;

        // Assert
        model.DemoInput.Should().NotContain("\\");
    }

    [Fact]
    public void OnnxModel_ShouldNormalizeDemoOutput_WhenSet()
    {
        // Arrange
        var model = TestDataBuilder.CreateOnnxModel();
        var pathWithBackslashes = "demo\\output\\test.jpg";

        // Act
        model.DemoOutput = pathWithBackslashes;

        // Assert
        model.DemoOutput.Should().NotContain("\\");
    }

    [Fact]
    public void OnnxModel_ShouldThrowException_WhenInvalidDemoInput()
    {
        // Arrange
        var model = TestDataBuilder.CreateOnnxModel();
        // Use a path with invalid characters that will cause UriFormatException
        var invalidPath = "http://[invalid-uri"; // Invalid URI format

        // Act
        var act = () => model.DemoInput = invalidPath;

        // Assert
        act.Should().Throw<InvalidUriException>();
    }
}
