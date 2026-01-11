using EzioHost.Shared.Models;
using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface IVideoApi
{
    [Get("/api/Video")]
    Task<List<VideoDto>> GetVideos(
        [Query] int? pageNumber = 1,
        [Query] int? pageSize = 20,
        [Query] bool? includeStreams = true,
        [Query] bool? includeUpscales = false);

    [Get("/api/Video/{videoId}")]
    Task<VideoDto> GetVideoById(Guid videoId);

    [Post("/api/Video")]
    Task UpdateVideo([Body] VideoUpdateDto video);

    [Delete("/api/Video/{videoId}")]
    Task DeleteVideo(Guid videoId);

    [Post("/api/video/{videoId}/upscale/{modelId}")]
    Task UpscaleVideo(Guid videoId, Guid modelId);
}