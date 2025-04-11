using EzioHost.Domain.Entities;
using System.Linq.Expressions;

namespace EzioHost.Core.Repositories
{
    public interface IUpscaleRepository
    {
        Task<VideoUpscale> AddNewVideoUpscale(VideoUpscale newVideoUpscale);
        Task<VideoUpscale?> GetVideoUpscaleById(Guid id);
        Task<VideoUpscale> UpdateVideoUpscale(VideoUpscale updateVideoUpscale);
        Task DeleteVideoUpscale(VideoUpscale deleteVideoUpscale);
        Task<IEnumerable<VideoUpscale>> GetVideoUpscales(Expression<Func<VideoUpscale, bool>>? expression = null, Expression<Func<VideoUpscale, object>>[]? includes = null);
        Task<VideoUpscale?> GetVideoNeedUpscale();
    }
}
