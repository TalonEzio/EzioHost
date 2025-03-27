using EzioHost.Domain.Entities;

namespace EzioHost.Core.Repositories
{
    public interface IVideoStreamRepository
    {
        void AddRange(IEnumerable<VideoStream> videoStreams);
        void Create(VideoStream videoStream);
    }
}
