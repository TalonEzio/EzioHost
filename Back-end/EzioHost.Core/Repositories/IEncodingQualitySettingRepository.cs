using EzioHost.Domain.Entities;

namespace EzioHost.Core.Repositories;

public interface IEncodingQualitySettingRepository
{
    Task<IEnumerable<EncodingQualitySetting>> GetSettingsByUserId(Guid userId);
    Task<IEnumerable<EncodingQualitySetting>> GetActiveSettingsByUserId(Guid userId);
    Task<EncodingQualitySetting?> GetSettingByUserIdAndResolution(Guid userId, EzioHost.Shared.Enums.VideoEnum.VideoResolution resolution);
    Task<EncodingQualitySetting> AddSetting(EncodingQualitySetting setting);
    Task<EncodingQualitySetting> UpdateSetting(EncodingQualitySetting setting);
    Task DeleteSetting(EncodingQualitySetting setting);
    Task<bool> UserHasSettings(Guid userId);
}
