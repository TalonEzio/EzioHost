using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Implement;
using EzioHost.Domain.Entities;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace EzioHost.UnitTests.Core.Services;

public class OnnxModelServiceTests
{
    private readonly Mock<IOnnxModelRepository> _onnxModelRepositoryMock;
    private readonly OnnxModelService _onnxModelService;

    public OnnxModelServiceTests()
    {
        _onnxModelRepositoryMock = TestMockFactory.CreateOnnxModelRepositoryMock();
        _onnxModelService = new OnnxModelService(_onnxModelRepositoryMock.Object);
    }

    [Fact]
    public async Task GetOnnxModels_ShouldReturnAllModels()
    {
        // Arrange
        var models = new List<OnnxModel>
        {
            TestDataBuilder.CreateOnnxModel(name: "Model1"),
            TestDataBuilder.CreateOnnxModel(name: "Model2")
        };

        _onnxModelRepositoryMock
            .Setup(x => x.GetOnnxModels())
            .ReturnsAsync(models);

        // Act
        var result = await _onnxModelService.GetOnnxModels();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _onnxModelRepositoryMock.Verify(x => x.GetOnnxModels(), Times.Once);
    }

    [Fact]
    public async Task GetOnnxModelById_ShouldReturnModel_WhenModelExists()
    {
        // Arrange
        var modelId = Guid.NewGuid();
        var expectedModel = TestDataBuilder.CreateOnnxModel(modelId);

        _onnxModelRepositoryMock
            .Setup(x => x.GetOnnxModelById(modelId))
            .ReturnsAsync(expectedModel);

        // Act
        var result = await _onnxModelService.GetOnnxModelById(modelId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedModel);
        _onnxModelRepositoryMock.Verify(x => x.GetOnnxModelById(modelId), Times.Once);
    }

    [Fact]
    public async Task GetOnnxModelById_ShouldReturnNull_WhenModelDoesNotExist()
    {
        // Arrange
        var modelId = Guid.NewGuid();

        _onnxModelRepositoryMock
            .Setup(x => x.GetOnnxModelById(modelId))
            .ReturnsAsync((OnnxModel?)null);

        // Act
        var result = await _onnxModelService.GetOnnxModelById(modelId);

        // Assert
        result.Should().BeNull();
        _onnxModelRepositoryMock.Verify(x => x.GetOnnxModelById(modelId), Times.Once);
    }

    [Fact]
    public async Task AddOnnxModel_ShouldReturnCreatedModel()
    {
        // Arrange
        var newModel = TestDataBuilder.CreateOnnxModel();

        _onnxModelRepositoryMock
            .Setup(x => x.AddOnnxModel(It.IsAny<OnnxModel>()))
            .ReturnsAsync(newModel);

        // Act
        var result = await _onnxModelService.AddOnnxModel(newModel);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(newModel);
        _onnxModelRepositoryMock.Verify(x => x.AddOnnxModel(newModel), Times.Once);
    }

    [Fact]
    public async Task UpdateOnnxModel_ShouldReturnUpdatedModel()
    {
        // Arrange
        var modelToUpdate = TestDataBuilder.CreateOnnxModel();
        modelToUpdate.Name = "Updated Model";

        _onnxModelRepositoryMock
            .Setup(x => x.UpdateOnnxModel(It.IsAny<OnnxModel>()))
            .ReturnsAsync(modelToUpdate);

        // Act
        var result = await _onnxModelService.UpdateOnnxModel(modelToUpdate);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Model");
        _onnxModelRepositoryMock.Verify(x => x.UpdateOnnxModel(modelToUpdate), Times.Once);
    }

    [Fact]
    public async Task DeleteOnnxModel_ById_ShouldCallRepository()
    {
        // Arrange
        var modelId = Guid.NewGuid();

        _onnxModelRepositoryMock
            .Setup(x => x.DeleteOnnxModel(modelId))
            .Returns(Task.CompletedTask);

        // Act
        await _onnxModelService.DeleteOnnxModel(modelId);

        // Assert
        _onnxModelRepositoryMock.Verify(x => x.DeleteOnnxModel(modelId), Times.Once);
    }

    [Fact]
    public async Task DeleteOnnxModel_ByModel_ShouldCallRepository()
    {
        // Arrange
        var modelToDelete = TestDataBuilder.CreateOnnxModel();

        _onnxModelRepositoryMock
            .Setup(x => x.DeleteOnnxModel(It.IsAny<OnnxModel>()))
            .Returns(Task.CompletedTask);

        // Act
        await _onnxModelService.DeleteOnnxModel(modelToDelete);

        // Assert
        _onnxModelRepositoryMock.Verify(x => x.DeleteOnnxModel(modelToDelete), Times.Once);
    }
}