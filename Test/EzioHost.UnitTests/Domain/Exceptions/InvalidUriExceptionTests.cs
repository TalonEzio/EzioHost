using EzioHost.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Exceptions;

public class InvalidUriExceptionTests
{
    [Fact]
    public void InvalidUriException_ShouldCreateWithMessage()
    {
        // Arrange
        var message = "Invalid URI path";

        // Act
        var exception = new InvalidUriException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void InvalidUriException_ShouldCreateWithMessageAndInnerException()
    {
        // Arrange
        var message = "Invalid URI path";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new InvalidUriException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }
}