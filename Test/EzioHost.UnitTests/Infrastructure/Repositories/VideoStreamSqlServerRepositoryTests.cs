using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.UnitTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class VideoStreamSqlServerRepositoryTests : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly VideoStreamSqlServerRepository _repository;

    public VideoStreamSqlServerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new VideoStreamSqlServerRepository(_dbContext);
    }

    [Fact]
    public void Add_ShouldAddVideoStreamToContext()
    {
        // Arrange
        var videoStream = TestDataBuilder.CreateVideoStream();

        // Act
        _repository.Add(videoStream);

        // Assert
        _dbContext.ChangeTracker.Entries<VideoStream>().Should().HaveCount(1);
    }

    [Fact]
    public void AddRange_ShouldAddMultipleVideoStreamsToContext()
    {
        // Arrange
        var videoStreams = new List<VideoStream>
        {
            TestDataBuilder.CreateVideoStream(),
            TestDataBuilder.CreateVideoStream(),
            TestDataBuilder.CreateVideoStream()
        };

        // Act
        _repository.AddRange(videoStreams);

        // Assert
        _dbContext.ChangeTracker.Entries<VideoStream>().Should().HaveCount(3);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
