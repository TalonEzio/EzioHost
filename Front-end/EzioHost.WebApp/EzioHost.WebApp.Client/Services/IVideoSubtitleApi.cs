using EzioHost.Shared.Models;
using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface IVideoSubtitleApi
{
    [Post("/api/VideoSubtitle/{videoId}")]
    [Multipart]
    Task<VideoSubtitleDto> UploadSubtitle(
        Guid videoId,
        [AliasAs("language")] string language,
        [AliasAs("file")] StreamPart file);

    [Get("/api/VideoSubtitle/{videoId}")]
    Task<List<VideoSubtitleDto>> GetSubtitles(Guid videoId);

    [Delete("/api/VideoSubtitle/{subtitleId}")]
    Task DeleteSubtitle(Guid subtitleId);
}