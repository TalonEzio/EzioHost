using EzioHost.Domain.Entities;

namespace EzioHost.Core.Repositories;

public interface ISubtitleTranscribeRepository
{
    Task<SubtitleTranscribe?> GetByIdAsync(Guid id);
    Task<SubtitleTranscribe?> GetNextJobAsync();
    Task<SubtitleTranscribe> AddAsync(SubtitleTranscribe transcribe);
    Task<SubtitleTranscribe> UpdateAsync(SubtitleTranscribe transcribe);
    Task<bool> ExistsByVideoIdAsync(Guid videoId);
}
