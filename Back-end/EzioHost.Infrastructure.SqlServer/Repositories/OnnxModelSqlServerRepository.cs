using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContext;
using Microsoft.EntityFrameworkCore;

namespace EzioHost.Infrastructure.SqlServer.Repositories
{
    public class OnnxModelSqlServerRepository(EzioHostDbContext dbContext) : IOnnxModelRepository
    {
        public Task<IEnumerable<OnnxModel>> GetOnnxModels()
        {
            return Task.FromResult(dbContext.OnnxModels.AsEnumerable());
        }

        public Task<OnnxModel?> GetOnnxModelById(Guid id)
        {
            return dbContext.OnnxModels.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<OnnxModel> AddOnnxModel(OnnxModel newModel)
        {
            dbContext.OnnxModels.Add(newModel);
            await dbContext.SaveChangesAsync();
            return newModel;
        }

        public async Task<OnnxModel> UpdateOnnxModel(OnnxModel updateModel)
        {
            dbContext.OnnxModels.Update(updateModel);
            await dbContext.SaveChangesAsync();
            return updateModel;
        }

        public async Task DeleteOnnxModel(Guid id)
        {
            var onnxModel = await GetOnnxModelById(id);
            if (onnxModel is not null)
            {
                dbContext.OnnxModels.Remove(onnxModel);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteOnnxModel(OnnxModel deleteModel)
        {
            dbContext.OnnxModels.Remove(deleteModel);
            await dbContext.SaveChangesAsync();

        }
    }
}
