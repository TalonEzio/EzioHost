using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories;

public class SubtitleTranscribeSettingSqlServerRepository(EzioHostDbContext dbContext) : ISubtitleTranscribeSettingRepository
{
    public Task<SubtitleTranscribeSetting?> GetByUserIdAsync(Guid userId)
    {
        return dbContext.SubtitleTranscribeSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<SubtitleTranscribeSetting> CreateOrUpdateAsync(SubtitleTranscribeSetting setting)
    {
        var existing = await dbContext.SubtitleTranscribeSettings
            .FirstOrDefaultAsync(x => x.UserId == setting.UserId);

        if (existing == null)
        {
            dbContext.SubtitleTranscribeSettings.Add(setting);
        }
        else
        {
            existing.IsEnabled = setting.IsEnabled;
            existing.ModelType = setting.ModelType;
            existing.UseGpu = setting.UseGpu;
            existing.GpuDeviceId = setting.GpuDeviceId;
            existing.ModifiedBy = setting.ModifiedBy;
            dbContext.SubtitleTranscribeSettings.Update(existing);
            setting = existing;
        }

        await dbContext.SaveChangesAsync();
        return setting;
    }
}
