using System.Linq.Expressions;
using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Domain.Enums;
using EzioHost.Infrastructure.SqlServer.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories
{
    public class VideoSqlServerRepository(EzioHostDbContext dbContext) : IVideoRepository
    {
        private readonly DbSet<Video> _videos = dbContext.Videos;
        public async Task<Video> AddNewVideo(Video newVideo)
        {
            _videos.Add(newVideo);
            await dbContext.SaveChangesAsync();

            return newVideo;
        }

        public Task<Video?> GetVideoById(Guid id)
        {
            var find = _videos.FirstOrDefaultAsync(x => x.Id == id);
            return find;
        }

        public async Task<Video> UpdateVideo(Video updateVideo)
        {
            var find = await GetVideoById(updateVideo.Id);
            if (find is null) throw new Exception("Not found video");

            find.RawLocation = updateVideo.RawLocation;
            find.M3U8Location = string.Empty;
            find.Resolution = updateVideo.Resolution;
            find.Status = VideoEnum.VideoStatus.Queue;
            find.Title = updateVideo.Title;

            await dbContext.SaveChangesAsync();
            return find;
        }
        
        public async Task DeleteVideo(Video deleteVideo)
        {
            var find = await GetVideoById(deleteVideo.Id);
            if (find != null)
            {
                _videos.Remove(find);
                await dbContext.SaveChangesAsync();
            }
        }

        public Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null, Expression<Func<Video, object>>[]? includes = null)
        {
            var videoQueryable = _videos.AsQueryable();
            if (includes is { Length: > 0 })
            {
                foreach (var include in includes)
                {
                    videoQueryable = _videos.Include(include);
                }
            }
            return Task.FromResult<IEnumerable<Video>>(expression != null ? videoQueryable.Where(expression) : videoQueryable);
        }

        public Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null)
        {
            return Task.FromResult<IEnumerable<Video>>(expression == null ? _videos : _videos.Where(expression));
        }
    }
}
