using EzioHost.Core.Repositories;

namespace EzioHost.Core.UnitOfWorks
{
    public interface IVideoUnitOfWork : IBaseUnitOfWork
    {
        public IVideoRepository VideoRepository { get; set; }
        public IVideoStreamRepository VideoStreamRepository { get; set; }
    }
}
