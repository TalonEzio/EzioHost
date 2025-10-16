using EzioHost.Core.Repositories;
using EzioHost.Domain.Entities;
using EzioHost.Infrastructure.SqlServer.DataContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EzioHost.Infrastructure.SqlServer.Repositories
{
    public class FileUploadSqlServerRepository(EzioHostDbContext dbContext) : IFileUploadRepository
    {
        public Task<FileUpload?> GetFileUploadByCondition(Expression<Func<FileUpload, bool>> expression)
        {
            return dbContext.FileUploads.FirstOrDefaultAsync(expression);
        }

        public Task<FileUpload?> GetFileUploadById(Guid id)
        {
            return dbContext.FileUploads.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<FileUpload> AddFileUpload(FileUpload fileUpload)
        {
            dbContext.FileUploads.Add(fileUpload);
            await dbContext.SaveChangesAsync();
            return fileUpload;
        }

        public async Task<FileUpload> UpdateFileUpload(FileUpload fileUpload)
        {
            dbContext.FileUploads.Update(fileUpload);
            await dbContext.SaveChangesAsync();
            return fileUpload;
        }

        public async Task DeleteFileUpload(Guid id)
        {
            var deleteFile = await dbContext.FileUploads.FirstOrDefaultAsync(x => x.Id == id);

            if (deleteFile != null)
            {
                dbContext.FileUploads.Remove(deleteFile);
                await dbContext.SaveChangesAsync();
            }
        }

        public Task DeleteFileUpload(FileUpload fileUpload)
        {
            dbContext.FileUploads.Remove(fileUpload);
            return dbContext.SaveChangesAsync();
        }
    }
}
