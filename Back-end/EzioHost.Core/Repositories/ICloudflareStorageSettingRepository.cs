using EzioHost.Domain.Entities;

namespace EzioHost.Core.Repositories;

public interface ICloudflareStorageSettingRepository
{
    Task<CloudflareStorageSetting?> GetByUserIdAsync(Guid userId);
    Task<CloudflareStorageSetting> CreateOrUpdateAsync(CloudflareStorageSetting setting);
}
