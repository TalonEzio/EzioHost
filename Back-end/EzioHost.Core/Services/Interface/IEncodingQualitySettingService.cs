using EzioHost.Shared.Models;

namespace EzioHost.Core.Services.Interface;

public interface IEncodingQualitySettingService
{
    Task<List<EncodingQualitySettingDto>> GetUserSettings(Guid userId);
    Task<List<EncodingQualitySettingDto>> UpdateUserSettings(Guid userId, EncodingQualitySettingUpdateRequest request);
    Task<List<EncodingQualitySettingDto>> GetActiveSettingsForEncoding(Guid userId);
}
