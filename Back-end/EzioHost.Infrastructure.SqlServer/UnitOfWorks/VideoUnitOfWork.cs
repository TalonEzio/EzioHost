using EzioHost.Core.Repositories;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Infrastructure.SqlServer.DataContext;

namespace EzioHost.Infrastructure.SqlServer.UnitOfWorks
{
    public class VideoUnitOfWork(
        EzioHostDbContext dbContext,
        IVideoRepository videoRepository,
        IVideoStreamRepository videoStreamRepository) : BaseUnitOfWork(dbContext), IVideoUnitOfWork
    {
        public IVideoRepository VideoRepository { get; set; } = videoRepository;
        public IVideoStreamRepository VideoStreamRepository { get; set; } = videoStreamRepository;
    }
}
