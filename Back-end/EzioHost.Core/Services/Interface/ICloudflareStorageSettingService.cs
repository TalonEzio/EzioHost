using EzioHost.Shared.Models;

namespace EzioHost.Core.Services.Interface;

public interface ICloudflareStorageSettingService
{
    Task<CloudflareStorageSettingDto> GetUserSettingsAsync(Guid userId);
    Task<CloudflareStorageSettingDto> UpdateUserSettingsAsync(Guid userId, CloudflareStorageSettingUpdateDto dto);
}
