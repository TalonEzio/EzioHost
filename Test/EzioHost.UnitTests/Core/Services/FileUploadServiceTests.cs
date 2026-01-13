using System.Linq.Expressions;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class FileUploadServiceTests
{
    private readonly Mock<IDirectoryProvider> _directoryProviderMock;
    private readonly Mock<IFileUploadRepository> _fileUploadRepositoryMock;
    private readonly FileUploadService _fileUploadService;
    private readonly Mock<IVideoService> _videoServiceMock;

    public FileUploadServiceTests()
    {
        _fileUploadRepositoryMock = TestMockFactory.CreateFileUploadRepositoryMock();
        _directoryProviderMock = TestMockFactory.CreateDirectoryProviderMock();
        _videoServiceMock = TestMockFactory.CreateVideoServiceMock();
        _fileUploadService = new FileUploadService(
            _fileUploadRepositoryMock.Object,
            _directoryProviderMock.Object,
            _videoServiceMock.Object);
    }

    [Fact]
    public void GetFileUploadDirectory_ShouldReturnCorrectPath()
    {
        // Arrange
        var fileUploadId = Guid.NewGuid();
        _directoryProviderMock.Setup(x => x.GetBaseUploadFolder()).Returns("uploads");

        // Act
        var result = _fileUploadService.GetFileUploadDirectory(fileUploadId);

        // Assert
        result.Should().Be(Path.Combine("uploads", fileUploadId.ToString()));
    }

    [Fact]
    public void GetFileUploadTempPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var fileUploadId = Guid.NewGuid();
        _directoryProviderMock.Setup(x => x.GetBaseUploadFolder()).Returns("uploads");

        // Act
        var result = _fileUploadService.GetFileUploadTempPath(fileUploadId);

        // Assert
        result.Should().Be(Path.Combine("uploads", fileUploadId.ToString(), $"{fileUploadId}.chunk"));
    }

    [Fact]
    public async Task GetFileUploadById_ShouldReturnFileUpload_WhenExists()
    {
        // Arrange
        var fileUploadId = Guid.NewGuid();
        var expectedFileUpload = new FileUpload { Id = fileUploadId };

        _fileUploadRepositoryMock
            .Setup(x => x.GetFileUploadById(fileUploadId))
            .ReturnsAsync(expectedFileUpload);

        // Act
        var result = await _fileUploadService.GetFileUploadById(fileUploadId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedFileUpload);
        _fileUploadRepositoryMock.Verify(x => x.GetFileUploadById(fileUploadId), Times.Once);
    }

    [Fact]
    public async Task GetFileUploadByCondition_ShouldReturnFileUpload_WhenExists()
    {
        // Arrange
        var fileUploadId = Guid.NewGuid();
        var expectedFileUpload = new FileUpload { Id = fileUploadId };
        Expression<Func<FileUpload, bool>> expression = f => f.Id == fileUploadId;

        _fileUploadRepositoryMock
            .Setup(x => x.GetFileUploadByCondition(It.IsAny<Expression<Func<FileUpload, bool>>>()))
            .ReturnsAsync(expectedFileUpload);

        // Act
        var result = await _fileUploadService.GetFileUploadByCondition(expression);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedFileUpload);
        _fileUploadRepositoryMock.Verify(x => x.GetFileUploadByCondition(expression), Times.Once);
    }

    [Fact]
    public async Task AddFileUpload_ShouldReturnCreatedFileUpload()
    {
        // Arrange
        var fileUpload = new FileUpload { Id = Guid.NewGuid() };

        _fileUploadRepositoryMock
            .Setup(x => x.AddFileUpload(It.IsAny<FileUpload>()))
            .ReturnsAsync(fileUpload);

        // Act
        var result = await _fileUploadService.AddFileUpload(fileUpload);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(fileUpload);
        _fileUploadRepositoryMock.Verify(x => x.AddFileUpload(fileUpload), Times.Once);
    }

    [Fact]
    public async Task UpdateFileUpload_ShouldReturnUpdatedFileUpload()
    {
        // Arrange
        var fileUpload = new FileUpload { Id = Guid.NewGuid() };

        _fileUploadRepositoryMock
            .Setup(x => x.UpdateFileUpload(It.IsAny<FileUpload>()))
            .ReturnsAsync(fileUpload);

        // Act
        var result = await _fileUploadService.UpdateFileUpload(fileUpload);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(fileUpload);
        _fileUploadRepositoryMock.Verify(x => x.UpdateFileUpload(fileUpload), Times.Once);
    }

    [Fact]
    public async Task DeleteFileUpload_ById_ShouldDeleteFromRepositoryAndDirectory()
    {
        // Arrange
        var fileUploadId = Guid.NewGuid();
        _directoryProviderMock.Setup(x => x.GetBaseUploadFolder()).Returns(Path.GetTempPath());
        _fileUploadRepositoryMock
            .Setup(x => x.DeleteFileUpload(fileUploadId))
            .Returns(Task.CompletedTask);

        // Create a temporary directory to simulate upload directory
        var uploadDir = Path.Combine(Path.GetTempPath(), fileUploadId.ToString());
        Directory.CreateDirectory(uploadDir);

        try
        {
            // Act
            await _fileUploadService.DeleteFileUpload(fileUploadId);

            // Assert
            _fileUploadRepositoryMock.Verify(x => x.DeleteFileUpload(fileUploadId), Times.Once);
            Directory.Exists(uploadDir).Should().BeFalse("Directory should be deleted");
        }
        finally
        {
            if (Directory.Exists(uploadDir))
                Directory.Delete(uploadDir);
        }
    }

    [Fact]
    public async Task DeleteFileUpload_ByEntity_ShouldDeleteFromRepositoryAndDirectory()
    {
        // Arrange
        var fileUpload = new FileUpload { Id = Guid.NewGuid() };
        _directoryProviderMock.Setup(x => x.GetBaseUploadFolder()).Returns(Path.GetTempPath());
        _fileUploadRepositoryMock
            .Setup(x => x.DeleteFileUpload(It.IsAny<FileUpload>()))
            .Returns(Task.CompletedTask);

        // Create a temporary directory to simulate upload directory
        var uploadDir = Path.Combine(Path.GetTempPath(), fileUpload.Id.ToString());
        Directory.CreateDirectory(uploadDir);

        try
        {
            // Act
            await _fileUploadService.DeleteFileUpload(fileUpload);

            // Assert
            _fileUploadRepositoryMock.Verify(x => x.DeleteFileUpload(fileUpload), Times.Once);
            Directory.Exists(uploadDir).Should().BeFalse("Directory should be deleted");
        }
        finally
        {
            if (Directory.Exists(uploadDir))
                Directory.Delete(uploadDir);
        }
    }
}