using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Shared.Enums;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class UpscaleSqlServerRepositoryTests_Additional : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly UpscaleSqlServerRepository _repository;

    public UpscaleSqlServerRepositoryTests_Additional()
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
    public async Task GetVideoNeedUpscale_ShouldReturnUpscale_WithQueueStatus()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var model = TestDataBuilder.CreateOnnxModel();
        _dbContext.Videos.Add(video);
        _dbContext.OnnxModels.Add(model);
        await _dbContext.SaveChangesAsync();

        var upscale = TestDataBuilder.CreateVideoUpscale(
            videoId: video.Id,
            modelId: model.Id,
            status: VideoEnum.VideoUpscaleStatus.Queue);
        upscale.Video = video;
        upscale.Model = model;
        _dbContext.VideoUpscales.Add(upscale);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetVideoNeedUpscale();

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(VideoEnum.VideoUpscaleStatus.Queue);
    }

    [Fact]
    public async Task DeleteVideoUpscale_ShouldRemoveFromDatabase()
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
        await _repository.DeleteVideoUpscale(upscale);

        // Assert
        var upscaleInDb = await _dbContext.VideoUpscales
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == upscale.Id);

        if (upscaleInDb != null)
            upscaleInDb.DeletedAt.Should().NotBeNull("Upscale should be soft deleted");
        else
            upscaleInDb.Should().BeNull();
    }
}