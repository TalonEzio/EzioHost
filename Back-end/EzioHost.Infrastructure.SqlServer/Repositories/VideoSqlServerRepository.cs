using System.Linq.Expressions;
using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories;

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
        dbContext.Videos.Update(updateVideo);
        await dbContext.SaveChangesAsync();
        return updateVideo;
    }

    public Task<Video> UpdateVideoForUnitOfWork(Video updateVideo)
    {
        _videos.Update(updateVideo);
        return Task.FromResult(updateVideo);
    }

    public async Task DeleteVideo(Video deleteVideo)
    {
        var find = await GetVideoById(deleteVideo.Id);
        if (find != null)
        {
            find.ShareType = VideoEnum.VideoShareType.Private; //block share
            _videos.Remove(find);
            await dbContext.SaveChangesAsync();
        }
    }

    public Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null,
        Expression<Func<Video, object>>[]? includes = null)
    {
        var videoQueryable = _videos.AsQueryable();
        if (includes is { Length: > 0 })
            foreach (var include in includes)
                videoQueryable = videoQueryable.Include(include);

        return Task.FromResult<IEnumerable<Video>>(expression != null
            ? videoQueryable.Where(expression)
            : videoQueryable);
    }

    public Task<Video?> GetVideoToEncode()
    {
        var video = _videos.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == VideoEnum.VideoStatus.Queue);
        return video;
    }

    public async Task<Video?> GetVideoByVideoStreamId(Guid videoStreamId)
    {
        var videoStream = await dbContext.VideoStreams.Include(x => x.Video)
            .FirstOrDefaultAsync(x => x.Id == videoStreamId);
        return videoStream?.Video;
    }

    public Task<Video?> GetVideoUpscaleById(Guid videoId)
    {
        return _videos.Include(x => x.VideoUpscales).FirstOrDefaultAsync(x =>
            x.Id == videoId && x.VideoUpscales.All(upscale => upscale.Status == VideoEnum.VideoUpscaleStatus.Ready));
    }

    public Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null)
    {
        return Task.FromResult<IEnumerable<Video>>(expression == null ? _videos : _videos.Where(expression));
    }
}