using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Infrastructure.SqlServer.Repositories;
using EzioHost.Shared.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EzioHost.UnitTests.Infrastructure.Repositories;

public class EncodingQualitySettingSqlServerRepositoryTests : IDisposable
{
    private readonly EzioHostDbContext _dbContext;
    private readonly EncodingQualitySettingSqlServerRepository _repository;

    public EncodingQualitySettingSqlServerRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EzioHostDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new EzioHostDbContext(options);
        _repository = new EncodingQualitySettingSqlServerRepository(_dbContext);
    }

    [Fact]
    public async Task GetSettingsByUserId_ShouldReturnSettings_WhenUserHasSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var setting = new EncodingQualitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Resolution = VideoEnum.VideoResolution._720p,
            BitrateKbps = 3000,
            IsEnabled = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        _dbContext.EncodingQualitySettings.Add(setting);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetSettingsByUserId(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(s => s.UserId == userId);
    }

    [Fact]
    public async Task GetActiveSettingsByUserId_ShouldReturnOnlyEnabledSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var enabledSetting = new EncodingQualitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Resolution = VideoEnum.VideoResolution._720p,
            BitrateKbps = 3000,
            IsEnabled = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        var disabledSetting = new EncodingQualitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Resolution = VideoEnum.VideoResolution._1080p,
            BitrateKbps = 5000,
            IsEnabled = false,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        _dbContext.EncodingQualitySettings.AddRange(enabledSetting, disabledSetting);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveSettingsByUserId(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(s => s.IsEnabled);
    }

    [Fact]
    public async Task UserHasSettings_ShouldReturnTrue_WhenUserHasSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var setting = new EncodingQualitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Resolution = VideoEnum.VideoResolution._720p,
            BitrateKbps = 3000,
            IsEnabled = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        _dbContext.EncodingQualitySettings.Add(setting);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.UserHasSettings(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserHasSettings_ShouldReturnFalse_WhenUserHasNoSettings()
    {
        // Act
        var result = await _repository.UserHasSettings(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddSetting_ShouldAddToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var setting = new EncodingQualitySetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Resolution = VideoEnum.VideoResolution._720p,
            BitrateKbps = 3000,
            IsEnabled = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        // Act
        var result = await _repository.AddSetting(setting);

        // Assert
        result.Should().NotBeNull();
        var settingInDb = await _dbContext.EncodingQualitySettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == result.Id);
        settingInDb.Should().NotBeNull();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
