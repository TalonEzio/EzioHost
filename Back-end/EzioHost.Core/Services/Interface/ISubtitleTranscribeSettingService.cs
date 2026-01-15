using EzioHost.Shared.Models;

namespace EzioHost.Core.Services.Interface;

public interface ISubtitleTranscribeSettingService
{
    Task<SubtitleTranscribeSettingDto> GetUserSettingsAsync(Guid userId);
    Task<SubtitleTranscribeSettingDto> UpdateUserSettingsAsync(Guid userId, SubtitleTranscribeSettingUpdateDto dto);
}
