using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;

namespace EzioHost.Core.Services.Interface;

public interface ISubtitleTranscribeService
{
    Task<SubtitleTranscribe> CreateTranscribeRequestAsync(Guid videoId, string language, Guid userId);
    Task<SubtitleTranscribe?> GetNextTranscribeJobAsync();
    Task ProcessTranscriptionAsync(SubtitleTranscribe transcribe);
    Task UpdateTranscribeStatusAsync(Guid id, VideoEnum.SubtitleTranscribeStatus status, string? errorMessage = null);
}
