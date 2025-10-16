using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContext;
using EzioHost.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EzioHost.Infrastructure.SqlServer.Repositories
{
    public class UpscaleSqlServerRepository(EzioHostDbContext dbContext) : IUpscaleRepository
    {
        private DbSet<VideoUpscale> VideoUpscales => dbContext.VideoUpscales;
        public async Task<VideoUpscale> AddNewVideoUpscale(VideoUpscale newVideoUpscale)
        {
            await VideoUpscales.AddAsync(newVideoUpscale);
            await dbContext.SaveChangesAsync();
            return newVideoUpscale;
        }

        public Task<VideoUpscale?> GetVideoUpscaleById(Guid id)
        {
            return VideoUpscales.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<VideoUpscale> UpdateVideoUpscale(VideoUpscale updateVideoUpscale)
        {
            VideoUpscales.Update(updateVideoUpscale);
            await dbContext.SaveChangesAsync();
            return updateVideoUpscale;
        }

        public Task DeleteVideoUpscale(VideoUpscale deleteVideoUpscale)
        {
            VideoUpscales.Remove(deleteVideoUpscale);
            return dbContext.SaveChangesAsync();
        }

        public Task<IEnumerable<VideoUpscale>> GetVideoUpscales(Expression<Func<VideoUpscale, bool>>? expression = null, Expression<Func<VideoUpscale, object>>[]? includes = null)
        {
            var queryable = VideoUpscales.AsQueryable();

            if (expression != null)
            {
                queryable = queryable.Where(expression);
            }
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    queryable = queryable.Include(include);
                }
            }
            return Task.FromResult(queryable.AsEnumerable());
        }

        public Task<VideoUpscale?> GetVideoNeedUpscale()
        {
            return VideoUpscales.Include(x => x.Video).Include(x => x.Model)
                .Where(x => x.Status == VideoEnum.VideoUpscaleStatus.Queue)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
