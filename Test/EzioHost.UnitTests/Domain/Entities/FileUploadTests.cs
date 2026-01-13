using EzioHost.Domain.Entities;
using EzioHost.Domain.Exceptions;
using EzioHost.Shared.Enums;
using FluentAssertions;
using Xunit;

namespace EzioHost.UnitTests.Domain.Entities;

public class FileUploadTests
{
    [Fact]
    public void FileUpload_IsCompleted_ShouldReturnTrue_WhenStatusIsCompletedAndBytesMatch()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4",
            FileSize = 1000,
            ReceivedBytes = 1000,
            Status = VideoEnum.FileUploadStatus.Completed
        };

        // Act & Assert
        fileUpload.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void FileUpload_IsCompleted_ShouldReturnFalse_WhenStatusIsNotCompleted()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4",
            FileSize = 1000,
            ReceivedBytes = 1000,
            Status = VideoEnum.FileUploadStatus.InProgress
        };

        // Act & Assert
        fileUpload.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void FileUpload_ShouldNormalizePhysicalPath_WhenSet()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4"
        };
        var pathWithBackslashes = "videos\\test\\file.mp4";

        // Act
        fileUpload.PhysicalPath = pathWithBackslashes;

        // Assert
        fileUpload.PhysicalPath.Should().NotContain("\\");
    }

    [Fact]
    public void FileUpload_ShouldThrowException_WhenInvalidPhysicalPath()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4"
        };
        // Use a path with invalid characters that will cause UriFormatException
        var invalidPath = "http://[invalid-uri"; // Invalid URI format

        // Act
        var act = () => fileUpload.PhysicalPath = invalidPath;

        // Assert
        act.Should().Throw<InvalidUriException>();
    }
}
