using EzioHost.Domain.Entities;

namespace EzioHost.Core.Repositories;

public interface ISubtitleTranscribeSettingRepository
{
    Task<SubtitleTranscribeSetting?> GetByUserIdAsync(Guid userId);
    Task<SubtitleTranscribeSetting> CreateOrUpdateAsync(SubtitleTranscribeSetting setting);
}
