using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories;

public class EncodingQualitySettingSqlServerRepository(EzioHostDbContext dbContext) : IEncodingQualitySettingRepository
{
    private readonly DbSet<EncodingQualitySetting> _settings = dbContext.EncodingQualitySettings;

    public Task<IEnumerable<EncodingQualitySetting>> GetSettingsByUserId(Guid userId)
    {
        return Task.FromResult<IEnumerable<EncodingQualitySetting>>(
            _settings
                .Where(s => s.UserId == userId && !s.DeletedAt.HasValue)
                .AsNoTracking()
                .ToList());
    }

    public Task<IEnumerable<EncodingQualitySetting>> GetActiveSettingsByUserId(Guid userId)
    {
        return Task.FromResult<IEnumerable<EncodingQualitySetting>>(
            _settings
                .Where(s => s.UserId == userId && s.IsEnabled && !s.DeletedAt.HasValue)
                .AsNoTracking()
                .ToList());
    }

    public Task<EncodingQualitySetting?> GetSettingByUserIdAndResolution(Guid userId,
        VideoEnum.VideoResolution resolution)
    {
        return _settings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Resolution == resolution && !s.DeletedAt.HasValue);
    }

    public async Task<EncodingQualitySetting> AddSetting(EncodingQualitySetting setting)
    {
        _settings.Add(setting);
        await dbContext.SaveChangesAsync();
        return setting;
    }

    public async Task<EncodingQualitySetting> UpdateSetting(EncodingQualitySetting setting)
    {
        _settings.Update(setting);
        await dbContext.SaveChangesAsync();
        return setting;
    }

    public async Task DeleteSetting(EncodingQualitySetting setting)
    {
        _settings.Remove(setting);
        await dbContext.SaveChangesAsync();
    }

    public Task<bool> UserHasSettings(Guid userId)
    {
        return _settings
            .AnyAsync(s => s.UserId == userId && !s.DeletedAt.HasValue);
    }
}