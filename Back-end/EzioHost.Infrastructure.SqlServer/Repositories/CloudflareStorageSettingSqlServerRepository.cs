using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories;

public class CloudflareStorageSettingSqlServerRepository(EzioHostDbContext dbContext) : ICloudflareStorageSettingRepository
{
    public Task<CloudflareStorageSetting?> GetByUserIdAsync(Guid userId)
    {
        return dbContext.CloudflareStorageSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<CloudflareStorageSetting> CreateOrUpdateAsync(CloudflareStorageSetting setting)
    {
        var existing = await dbContext.CloudflareStorageSettings
            .FirstOrDefaultAsync(x => x.UserId == setting.UserId);

        if (existing == null)
        {
            dbContext.CloudflareStorageSettings.Add(setting);
        }
        else
        {
            existing.IsEnabled = setting.IsEnabled;
            existing.ModifiedBy = setting.ModifiedBy;
            dbContext.CloudflareStorageSettings.Update(existing);
            setting = existing;
        }

        await dbContext.SaveChangesAsync();
        return setting;
    }
}
