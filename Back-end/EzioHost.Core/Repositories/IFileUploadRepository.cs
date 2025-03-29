using System.Linq.Expressions;
using EzioHost.Domain.Entities;

namespace EzioHost.Core.Repositories
{
    public interface IFileUploadRepository
    {
        Task<FileUpload?> GetFileUploadByCondition(Expression<Func<FileUpload,bool>> expression);
        Task<FileUpload?> GetFileUploadById(Guid id);
        Task<FileUpload> AddFileUpload(FileUpload fileUpload);
        Task<FileUpload> UpdateFileUpload(FileUpload fileUpload);
        Task DeleteFileUpload(Guid id);
        Task DeleteFileUpload(FileUpload fileUpload);
    }
}
