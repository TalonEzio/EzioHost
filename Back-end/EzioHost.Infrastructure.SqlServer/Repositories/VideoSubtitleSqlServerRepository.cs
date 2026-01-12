using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories;

public class VideoSubtitleSqlServerRepository(EzioHostDbContext dbContext) : IVideoSubtitleRepository
{
    public async Task<IEnumerable<VideoSubtitle>> GetSubtitlesByVideoId(Guid videoId)
    {
        return await dbContext.VideoSubtitles
            .AsNoTracking()
            .Where(x => x.VideoId == videoId)
            .ToListAsync();
    }

    public Task<VideoSubtitle?> GetSubtitleById(Guid id)
    {
        return dbContext.VideoSubtitles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<VideoSubtitle> AddSubtitle(VideoSubtitle subtitle)
    {
        dbContext.VideoSubtitles.Add(subtitle);
        await dbContext.SaveChangesAsync();
        return subtitle;
    }

    public async Task<VideoSubtitle> UpdateSubtitle(VideoSubtitle subtitle)
    {
        dbContext.VideoSubtitles.Update(subtitle);
        await dbContext.SaveChangesAsync();
        return subtitle;
    }

    public async Task DeleteSubtitle(VideoSubtitle subtitle)
    {
        var deleteSubtitle = await dbContext.VideoSubtitles.FirstOrDefaultAsync(x => x.Id == subtitle.Id);
        if (deleteSubtitle != null)
        {
            dbContext.VideoSubtitles.Remove(deleteSubtitle);
            await dbContext.SaveChangesAsync();
        }
    }
}
