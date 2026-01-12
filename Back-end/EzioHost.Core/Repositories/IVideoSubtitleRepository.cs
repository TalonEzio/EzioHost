using EzioHost.Domain.Entities;

namespace EzioHost.Core.Repositories;

public interface IVideoSubtitleRepository
{
    Task<IEnumerable<VideoSubtitle>> GetSubtitlesByVideoId(Guid videoId);
    Task<VideoSubtitle?> GetSubtitleById(Guid id);
    Task<VideoSubtitle> AddSubtitle(VideoSubtitle subtitle);
    Task<VideoSubtitle> UpdateSubtitle(VideoSubtitle subtitle);
    Task DeleteSubtitle(VideoSubtitle subtitle);
}
