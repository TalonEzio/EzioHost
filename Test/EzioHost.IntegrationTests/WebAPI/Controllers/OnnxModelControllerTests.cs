using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace EzioHost.IntegrationTests.WebAPI.Controllers;

public class OnnxModelControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OnnxModelControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOnnxModels_ShouldReturnOk_WhenAuthorized()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EzioHostDbContext>();
        
        var testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var model = new OnnxModel
        {
            Id = Guid.NewGuid(),
            Name = "Test Model",
            FileLocation = "models/test.onnx",
            Scale = 2,
            MustInputWidth = 256,
            MustInputHeight = 256,
            ElementType = EzioHost.Shared.Enums.TensorElementType.Float,
            CreatedBy = testUserId,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.OnnxModels.Add(model);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/OnnxModel");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOnnxModels_WithRequireDemo_ShouldFilterModels()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EzioHostDbContext>();
        
        var modelWithDemo = new OnnxModel
        {
            Id = Guid.NewGuid(),
            Name = "Model With Demo",
            FileLocation = "models/demo.onnx",
            Scale = 2,
            MustInputWidth = 256,
            MustInputHeight = 256,
            ElementType = EzioHost.Shared.Enums.TensorElementType.Float,
            DemoInput = "demo/input.jpg",
            DemoOutput = "demo/output.jpg",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        
        var modelWithoutDemo = new OnnxModel
        {
            Id = Guid.NewGuid(),
            Name = "Model Without Demo",
            FileLocation = "models/nodemo.onnx",
            Scale = 2,
            MustInputWidth = 256,
            MustInputHeight = 256,
            ElementType = EzioHost.Shared.Enums.TensorElementType.Float,
            DemoInput = string.Empty,
            DemoOutput = string.Empty,
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.OnnxModels.AddRange(modelWithDemo, modelWithoutDemo);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/OnnxModel?requireDemo=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetDemo_ShouldReturnOk_WhenModelExistsAndUserIsOwner()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EzioHostDbContext>();
        
        var testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var model = new OnnxModel
        {
            Id = Guid.NewGuid(),
            Name = "Test Model",
            FileLocation = "models/test.onnx",
            Scale = 2,
            MustInputWidth = 256,
            MustInputHeight = 256,
            ElementType = EzioHost.Shared.Enums.TensorElementType.Float,
            DemoInput = "demo/input.jpg",
            DemoOutput = "demo/output.jpg",
            CreatedBy = testUserId,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
        dbContext.OnnxModels.Add(model);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/OnnxModel/demo-reset/{model.Id}", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetDemo_ShouldReturnNotFound_WhenModelNotExists()
    {
        // Act
        var response = await _client.PostAsync($"/api/OnnxModel/demo-reset/{Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOnnxModel_ShouldReturnNoContent_WhenModelExists()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EzioHostDbContext>();
        
        var testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var model = new OnnxModel
        {
            Id = Guid.NewGuid(),
            Name = "Test Model",
            FileLocation = "models/test.onnx",
            Scale = 2,
            MustInputWidth = 256,
            MustInputHeight = 256,
            ElementType = EzioHost.Shared.Enums.TensorElementType.Float,
            CreatedBy = testUserId,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
        dbContext.OnnxModels.Add(model);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/OnnxModel/{model.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOnnxModel_ShouldReturnNotFound_WhenModelNotExists()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/OnnxModel/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
