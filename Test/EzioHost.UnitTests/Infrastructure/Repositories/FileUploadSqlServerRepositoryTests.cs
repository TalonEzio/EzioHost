using System.Linq.Expressions;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Shared.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class FileUploadSqlServerRepositoryTests : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly FileUploadSqlServerRepository _repository;

    public FileUploadSqlServerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new FileUploadSqlServerRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetFileUploadById_ShouldReturnFileUpload_WhenExists()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4",
            FileSize = 1024,
            Status = VideoEnum.FileUploadStatus.Pending
        };
        _dbContext.FileUploads.Add(fileUpload);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetFileUploadById(fileUpload.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(fileUpload.Id);
    }

    [Fact]
    public async Task GetFileUploadByCondition_ShouldReturnFileUpload_WhenExists()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4",
            FileSize = 1024,
            Status = VideoEnum.FileUploadStatus.Pending
        };
        _dbContext.FileUploads.Add(fileUpload);
        await _dbContext.SaveChangesAsync();

        Expression<Func<FileUpload, bool>> expression = f => f.FileName == "test.mp4";

        // Act
        var result = await _repository.GetFileUploadByCondition(expression);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("test.mp4");
    }

    [Fact]
    public async Task AddFileUpload_ShouldAddToDatabase()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4",
            FileSize = 1024,
            Status = VideoEnum.FileUploadStatus.Pending
        };

        // Act
        var result = await _repository.AddFileUpload(fileUpload);

        // Assert
        result.Should().NotBeNull();
        var fileInDb = await _dbContext.FileUploads.FindAsync(result.Id);
        fileInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateFileUpload_ShouldUpdateInDatabase()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4",
            FileSize = 1024,
            Status = VideoEnum.FileUploadStatus.Pending
        };
        _dbContext.FileUploads.Add(fileUpload);
        await _dbContext.SaveChangesAsync();

        fileUpload.Status = VideoEnum.FileUploadStatus.Completed;

        // Act
        var result = await _repository.UpdateFileUpload(fileUpload);

        // Assert
        result.Status.Should().Be(VideoEnum.FileUploadStatus.Completed);
    }

    [Fact]
    public async Task DeleteFileUpload_ById_ShouldRemoveFromDatabase()
    {
        // Arrange
        var fileUpload = new FileUpload
        {
            Id = Guid.NewGuid(),
            FileName = "test.mp4",
            FileSize = 1024
        };
        _dbContext.FileUploads.Add(fileUpload);
        await _dbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteFileUpload(fileUpload.Id);

        // Assert
        var fileInDb = await _dbContext.FileUploads.FindAsync(fileUpload.Id);
        fileInDb.Should().BeNull();
    }
}