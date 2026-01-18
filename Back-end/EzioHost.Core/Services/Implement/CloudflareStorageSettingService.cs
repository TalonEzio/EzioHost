using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;

namespace EzioHost.Core.Services.Implement;

public class CloudflareStorageSettingService(
    ICloudflareStorageSettingRepository repository) : ICloudflareStorageSettingService
{
    public async Task<CloudflareStorageSettingDto> GetUserSettingsAsync(Guid userId)
    {
        var setting = await repository.GetByUserIdAsync(userId);

        if (setting == null)
        {
            // Create default settings
            setting = new CloudflareStorageSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsEnabled = true,
                CreatedBy = userId,
                ModifiedBy = userId
            };

            setting = await repository.CreateOrUpdateAsync(setting);
        }

        return new CloudflareStorageSettingDto
        {
            Id = setting.Id,
            IsEnabled = setting.IsEnabled
        };
    }

    public async Task<CloudflareStorageSettingDto> UpdateUserSettingsAsync(Guid userId,
        CloudflareStorageSettingUpdateDto dto)
    {
        var setting = await repository.GetByUserIdAsync(userId);

        if (setting == null)
        {
            setting = new CloudflareStorageSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedBy = userId,
                ModifiedBy = userId
            };
        }

        setting.IsEnabled = dto.IsEnabled;
        setting.ModifiedBy = userId;

        setting = await repository.CreateOrUpdateAsync(setting);

        return new CloudflareStorageSettingDto
        {
            Id = setting.Id,
            IsEnabled = setting.IsEnabled
        };
    }
}
