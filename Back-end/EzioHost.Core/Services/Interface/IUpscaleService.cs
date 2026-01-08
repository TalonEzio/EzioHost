using EzioHost.Domain.Entities;
using EzioHost.Shared.Events;

namespace EzioHost.Core.Services.Interface;

public interface IUpscaleService
{
    Task UpscaleImage(OnnxModel model, string inputPath, string outputPath);
    Task UpscaleVideo(VideoUpscale videoUpscale);
    Task<VideoUpscale> AddNewVideoUpscale(VideoUpscale newVideoUpscale);
    Task<VideoUpscale?> GetVideoUpscaleById(Guid id);
    Task<VideoUpscale> UpdateVideoUpscale(VideoUpscale updateVideoUpscale);
    Task DeleteVideoUpscale(VideoUpscale deleteVideoUpscale);
    Task<VideoUpscale?> GetVideoNeedUpscale();

    event Action<VideoStreamAddedEventArgs> OnVideoUpscaleStreamAdded;
}