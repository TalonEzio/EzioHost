using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContexts;
using EzioHost.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories;

public class SubtitleTranscribeSqlServerRepository(EzioHostDbContext dbContext) : ISubtitleTranscribeRepository
{
    public Task<SubtitleTranscribe?> GetByIdAsync(Guid id)
    {
        return dbContext.SubtitleTranscribes
            .AsNoTracking()
            .Include(x => x.Video)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<SubtitleTranscribe?> GetNextJobAsync()
    {
        return dbContext.SubtitleTranscribes
            .Include(x => x.Video)
            .Where(x => x.Status == VideoEnum.SubtitleTranscribeStatus.Queue)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<SubtitleTranscribe> AddAsync(SubtitleTranscribe transcribe)
    {
        dbContext.SubtitleTranscribes.Add(transcribe);
        await dbContext.SaveChangesAsync();
        return transcribe;
    }

    public async Task<SubtitleTranscribe> UpdateAsync(SubtitleTranscribe transcribe)
    {
        dbContext.SubtitleTranscribes.Update(transcribe);
        await dbContext.SaveChangesAsync();
        return transcribe;
    }

    public Task<bool> ExistsByVideoIdAsync(Guid videoId)
    {
        return dbContext.SubtitleTranscribes
            .AnyAsync(x => x.VideoId == videoId && 
                          (x.Status == VideoEnum.SubtitleTranscribeStatus.Queue || 
                           x.Status == VideoEnum.SubtitleTranscribeStatus.Processing));
    }
}
