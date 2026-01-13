using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Shared.Enums;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class VideoSqlServerRepositoryTests : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly VideoSqlServerRepository _repository;

    public VideoSqlServerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new VideoSqlServerRepository(_dbContext);
    }

    [Fact]
    public async Task AddNewVideo_ShouldAddToDatabase()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();

        // Act
        var result = await _repository.AddNewVideo(video);

        // Assert
        result.Should().NotBeNull();
        var videoInDb = await _dbContext.Videos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == result.Id);
        videoInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task GetVideoById_ShouldReturnVideo_WhenExists()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVideoById(video.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(video.Id);
    }

    [Fact]
    public async Task GetVideoById_ShouldReturnNull_WhenDoesNotExist()
    {
        // Act
        var result = await _repository.GetVideoById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateVideo_ShouldUpdateInDatabase()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync();

        video.Title = "Updated Title";

        // Act
        var result = await _repository.UpdateVideo(video);

        // Assert
        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task DeleteVideo_ShouldRemoveFromDatabase()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteVideo(video);

        // Assert
        var videoInDb = await _dbContext.Videos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == video.Id);
        if (videoInDb != null)
        {
            videoInDb.DeletedAt.Should().NotBeNull();
        }
        else
        {
            videoInDb.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetVideos_ShouldReturnVideos_WithPagination()
    {
        // Arrange
        var videos = new List<Video>
        {
            TestDataBuilder.CreateVideo(title: "Video 1"),
            TestDataBuilder.CreateVideo(title: "Video 2"),
            TestDataBuilder.CreateVideo(title: "Video 3")
        };
        _dbContext.Videos.AddRange(videos);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVideos(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetVideos_ShouldFilterByExpression()
    {
        // Arrange
        var video1 = TestDataBuilder.CreateVideo(title: "Public Video", shareType: VideoEnum.VideoShareType.Public);
        var video2 = TestDataBuilder.CreateVideo(title: "Private Video", shareType: VideoEnum.VideoShareType.Private);
        _dbContext.Videos.AddRange(video1, video2);
        await _dbContext.SaveChangesAsync();

        Expression<Func<Video, bool>> expression = v => v.ShareType == VideoEnum.VideoShareType.Public;

        // Act
        var result = await _repository.GetVideos(1, 10, expression);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(v => v.ShareType == VideoEnum.VideoShareType.Public);
    }

    [Fact]
    public async Task GetVideoToEncode_ShouldReturnVideo_WithQueueStatus()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo(status: VideoEnum.VideoStatus.Queue);
        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVideoToEncode();

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(VideoEnum.VideoStatus.Queue);
    }

    [Fact]
    public async Task GetVideoToBackup_ShouldUpdateStatus_ToBackingUp()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo(backupStatus: VideoEnum.VideoBackupStatus.NotBackedUp);
        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVideoToBackup();

        // Assert
        result.Should().NotBeNull();
        result!.BackupStatus.Should().Be(VideoEnum.VideoBackupStatus.BackingUp);
    }

    [Fact]
    public async Task GetVideoByVideoStreamId_ShouldReturnVideo_WhenStreamExists()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        _dbContext.Videos.Add(video);
        await _dbContext.SaveChangesAsync();
        
        var stream = TestDataBuilder.CreateVideoStream(videoId: video.Id);
        stream.Video = video;
        _dbContext.VideoStreams.Add(stream);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetVideoByVideoStreamId(stream.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(video.Id);
    }

    [Fact]
    public async Task GetVideoWithReadyUpscale_ShouldReturnVideo_WithReadyUpscales()
    {
        // Arrange
        var video = TestDataBuilder.CreateVideo();
        var model = TestDataBuilder.CreateOnnxModel();
        var upscale = TestDataBuilder.CreateVideoUpscale(
            videoId: video.Id, 
            modelId: model.Id, 
            status: VideoEnum.VideoUpscaleStatus.Ready);
        _dbContext.Videos.Add(video);
        _dbContext.OnnxModels.Add(model);
        _dbContext.VideoUpscales.Add(upscale);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetVideoWithReadyUpscale(video.Id);

        // Assert
        result.Should().NotBeNull();
        result!.VideoUpscales.Should().Contain(u => u.Status == VideoEnum.VideoUpscaleStatus.Ready);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
