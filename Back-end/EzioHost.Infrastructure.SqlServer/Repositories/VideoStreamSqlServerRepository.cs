using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;

namespace EzioHost.Infrastructure.SqlServer.Repositories
{
    public class VideoStreamSqlServerRepository(EzioHostDbContext dbContext) : IVideoStreamRepository
    {
        public void AddRange(IEnumerable<VideoStream> videoStreams)
        {
            dbContext.VideoStreams.AddRange(videoStreams);
        }

        public void Create(VideoStream videoStream)
        {
            dbContext.VideoStreams.Add(videoStream);
        }
    }
}
