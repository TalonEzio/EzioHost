using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class OnnxModelSqlServerRepositoryTests : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly OnnxModelSqlServerRepository _repository;

    public OnnxModelSqlServerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new OnnxModelSqlServerRepository(_dbContext);
    }

    [Fact]
    public async Task GetOnnxModels_ShouldReturnAllModels()
    {
        // Arrange
        var model1 = TestDataBuilder.CreateOnnxModel(name: "Model1");
        var model2 = TestDataBuilder.CreateOnnxModel(name: "Model2");
        _dbContext.OnnxModels.AddRange(model1, model2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetOnnxModels();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOnnxModelById_ShouldReturnModel_WhenModelExists()
    {
        // Arrange
        var model = TestDataBuilder.CreateOnnxModel();
        _dbContext.OnnxModels.Add(model);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetOnnxModelById(model.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(model.Id);
        result.Name.Should().Be(model.Name);
    }

    [Fact]
    public async Task GetOnnxModelById_ShouldReturnNull_WhenModelDoesNotExist()
    {
        // Act
        var result = await _repository.GetOnnxModelById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddOnnxModel_ShouldAddModelToDatabase()
    {
        // Arrange
        var newModel = TestDataBuilder.CreateOnnxModel();

        // Act
        var result = await _repository.AddOnnxModel(newModel);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        
        var modelInDb = await _dbContext.OnnxModels.FindAsync(result.Id);
        modelInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateOnnxModel_ShouldUpdateModelInDatabase()
    {
        // Arrange
        var model = TestDataBuilder.CreateOnnxModel();
        _dbContext.OnnxModels.Add(model);
        await _dbContext.SaveChangesAsync();

        model.Name = "Updated Model";
        model.ModifiedAt = DateTime.UtcNow;

        // Act
        var result = await _repository.UpdateOnnxModel(model);

        // Assert
        result.Name.Should().Be("Updated Model");
        
        var modelInDb = await _dbContext.OnnxModels.FindAsync(model.Id);
        modelInDb.Should().NotBeNull();
        modelInDb!.Name.Should().Be("Updated Model");
    }

    [Fact]
    public async Task DeleteOnnxModel_ById_ShouldRemoveModelFromDatabase()
    {
        // Arrange
        var model = TestDataBuilder.CreateOnnxModel();
        _dbContext.OnnxModels.Add(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear(); // Clear tracking

        // Act
        await _repository.DeleteOnnxModel(model.Id);

        // Assert
        // Repository uses Remove() which should physically delete
        // But DbContext might convert to soft delete, so check both ways
        var modelInDb = await _dbContext.OnnxModels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == model.Id);
        
        // If soft delete, DeletedAt should be set; if hard delete, should be null
        if (modelInDb != null)
        {
            modelInDb.DeletedAt.Should().NotBeNull("Model should be soft deleted");
        }
        else
        {
            // Model was physically removed
            modelInDb.Should().BeNull();
        }
    }

    [Fact]
    public async Task DeleteOnnxModel_ByModel_ShouldRemoveModelFromDatabase()
    {
        // Arrange
        var model = TestDataBuilder.CreateOnnxModel();
        _dbContext.OnnxModels.Add(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear(); // Clear tracking

        // Act
        await _repository.DeleteOnnxModel(model);

        // Assert
        // Repository uses Remove() which should physically delete
        // But DbContext might convert to soft delete, so check both ways
        var modelInDb = await _dbContext.OnnxModels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == model.Id);
        
        // If soft delete, DeletedAt should be set; if hard delete, should be null
        if (modelInDb != null)
        {
            modelInDb.DeletedAt.Should().NotBeNull("Model should be soft deleted");
        }
        else
        {
            // Model was physically removed
            modelInDb.Should().BeNull();
        }
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
