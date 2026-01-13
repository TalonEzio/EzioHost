using System.Linq.Expressions;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Shared.Enums;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class UpscaleSqlServerRepositoryTests : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly UpscaleSqlServerRepository _repository;

    public UpscaleSqlServerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new UpscaleSqlServerRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task AddNewVideoUpscale_ShouldAddToDatabase()
    {
        // Arrange
        var upscale = TestDataBuilder.CreateVideoUpscale();

        // Act
        var result = await _repository.AddNewVideoUpscale(upscale);

        // Assert
        result.Should().NotBeNull();
        var upscaleInDb = await _dbContext.VideoUpscales
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == result.Id);
        upscaleInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task GetVideoUpscaleById_ShouldReturnUpscale_WhenExists()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var model = TestDataBuilder.CreateOnnxModel();
        _dbContext.Videos.Add(video);
        _dbContext.OnnxModels.Add(model);
        await _dbContext.SaveChangesAsync();

        var upscale = TestDataBuilder.CreateVideoUpscale(videoId: video.Id, modelId: model.Id);
        upscale.Video = video;
        upscale.Model = model;
        _dbContext.VideoUpscales.Add(upscale);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetVideoUpscaleById(upscale.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(upscale.Id);
    }

    [Fact]
    public async Task UpdateVideoUpscale_ShouldUpdateInDatabase()
    {
        // Arrange
        var upscale = TestDataBuilder.CreateVideoUpscale();
        _dbContext.VideoUpscales.Add(upscale);
        await _dbContext.SaveChangesAsync();

        upscale.Status = VideoEnum.VideoUpscaleStatus.Ready;

        // Act
        var result = await _repository.UpdateVideoUpscale(upscale);

        // Assert
        result.Status.Should().Be(VideoEnum.VideoUpscaleStatus.Ready);
    }

    [Fact]
    public async Task GetVideoUpscales_ShouldReturnUpscales_WhenExpressionMatches()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var model = TestDataBuilder.CreateOnnxModel();
        _dbContext.Videos.Add(video);
        _dbContext.OnnxModels.Add(model);
        await _dbContext.SaveChangesAsync();

        var upscale1 = TestDataBuilder.CreateVideoUpscale(videoId: video.Id, modelId: model.Id,
            status: VideoEnum.VideoUpscaleStatus.Queue);
        var upscale2 = TestDataBuilder.CreateVideoUpscale(videoId: video.Id, modelId: model.Id,
            status: VideoEnum.VideoUpscaleStatus.Ready);
        upscale1.Video = video;
        upscale1.Model = model;
        upscale2.Video = video;
        upscale2.Model = model;
        _dbContext.VideoUpscales.AddRange(upscale1, upscale2);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        Expression<Func<VideoUpscale, bool>> expression = u => u.Status == VideoEnum.VideoUpscaleStatus.Queue;

        // Act
        var result = await _repository.GetVideoUpscales(expression);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(u => u.Status == VideoEnum.VideoUpscaleStatus.Queue);
    }
}