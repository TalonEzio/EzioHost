using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Interface;

public interface IVideoSubtitleService
{
    Task<VideoSubtitle> UploadSubtitleAsync(Guid videoId, string language, Stream fileStream, string fileName, long fileSize, Guid userId);
    Task<IEnumerable<VideoSubtitle>> GetSubtitlesByVideoIdAsync(Guid videoId);
    Task<VideoSubtitle?> GetSubtitleByIdAsync(Guid subtitleId);
    Task DeleteSubtitleAsync(Guid subtitleId);
    Task<Stream> GetSubtitleFileStreamAsync(Guid subtitleId);
}
