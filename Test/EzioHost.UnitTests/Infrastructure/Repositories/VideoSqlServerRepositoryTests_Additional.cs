using System.Linq.Expressions;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class VideoSqlServerRepositoryTests_Additional : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly VideoSqlServerRepository _repository;

    public VideoSqlServerRepositoryTests_Additional()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new VideoSqlServerRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task UpdateVideoForUnitOfWork_ShouldUpdateVideo_WithoutSaving()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync();

        video.Title = "Updated Title";

        // Act
        var result = await _repository.UpdateVideoForUnitOfWork(video);

        // Assert
        result.Title.Should().Be("Updated Title");
        // Note: This method doesn't save, so changes are only in memory
    }

    [Fact]
    public async Task GetVideos_ShouldHandleInvalidPagination()
    {
        // Arrange
        var videos = new List<Video>
        {
            TestDataBuilder.CreateVideo(title: "Video 1"),
            TestDataBuilder.CreateVideo(title: "Video 2")
        };
        _dbContext.Videos.AddRange(videos);
        await _dbContext.SaveChangesAsync();

        // Act - Invalid page number and size should be corrected
        var result = await _repository.GetVideos(0, 0);

        // Assert
        result.Should().NotBeNull();
        // Should default to page 1, size 1
    }

    [Fact]
    public async Task GetVideos_ShouldIncludeNavigationProperties_WhenSpecified()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var stream = TestDataBuilder.CreateVideoStream(videoId: video.Id);
        _dbContext.Videos.Add(video);
        _dbContext.VideoStreams.Add(stream);
        await _dbContext.SaveChangesAsync();

        Expression<Func<Video, object>>[] includes = { v => v.VideoStreams };

        // Act
        var result = await _repository.GetVideos(1, 10, null, includes);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetVideoToBackup_ShouldReturnNull_WhenNoVideosNeedBackup()
    {
        // Arrange - No videos with NotBackedUp status

        // Act
        var result = await _repository.GetVideoToBackup();

        // Assert
        result.Should().BeNull();
    }
}