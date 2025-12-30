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
        var find = _videos.Include(video => video.VideoStreams).Include(video => video.VideoUpscales).FirstOrDefaultAsync(x => x.Id == id);
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

    public async Task<IEnumerable<Video>> GetVideos(
        int pageNumber,
        int pageSize,
        Expression<Func<Video, bool>>? expression = null,
        Expression<Func<Video, object>>[]? includes = null)
    {
        var videoQueryable = _videos.AsQueryable();
        if (includes is { Length: > 0 })
            foreach (var include in includes)
                videoQueryable = videoQueryable.Include(include);

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;

        if (expression != null) videoQueryable = videoQueryable.Where(expression);

        videoQueryable = videoQueryable.OrderByDescending(x => x.CreatedAt);

        videoQueryable = videoQueryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return await videoQueryable.AsNoTracking().ToListAsync();
    }

    public Task<Video?> GetVideoToEncode()
    {
        return _videos.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == VideoEnum.VideoStatus.Queue);
    }

    public async Task<Video?> GetVideoByVideoStreamId(Guid videoStreamId)
    {
        var stream = await dbContext.VideoStreams
            .AsNoTracking()
            .Include(vs => vs.Video)
            .FirstOrDefaultAsync(vs => vs.Id == videoStreamId);

        if (stream == null) return null;

        var video = stream.Video;
        video.VideoStreams = [stream];

        return video;
    }


    public Task<Video?> GetVideoWithReadyUpscale(Guid videoId)
    {
        return _videos
            .AsNoTracking()
            .Where(x => x.Id == videoId)
            .Include(x => x.VideoUpscales
                .Where(upscale => upscale.Status == VideoEnum.VideoUpscaleStatus.Ready))
            .FirstOrDefaultAsync();
    }
}