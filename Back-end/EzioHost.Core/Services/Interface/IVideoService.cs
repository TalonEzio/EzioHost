using System.Linq.Expressions;
using EzioHost.Domain.Entities;
using EzioHost.Domain.Events;

namespace EzioHost.Core.Services.Interface
{
    public interface IVideoService
    {
        Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null, Expression<Func<Video, object>>[]? includes = null);

        public event Action<VideoChangedEvent>? VideoChanged;
        Task<Video> AddNewVideo(Video newVideo);
        Task<Video> UpdateVideo(Video updateVideo);
        Task EncodeVideo(Video inputVideo);
        Task DeleteVideo(Video deleteVideo);
    }
}
