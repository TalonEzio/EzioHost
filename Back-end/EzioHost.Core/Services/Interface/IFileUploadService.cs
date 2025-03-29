using System.Linq.Expressions;
using EzioHost.Domain.Entities;
using EzioHost.Domain.Enums;

namespace EzioHost.Core.Services.Interface
{
    public interface IFileUploadService
    {
        string GetFileUploadDirectory(Guid fileUploadId);
        string GetFileUploadTempPath(Guid fileUploadId);
        Task<FileUpload?> GetFileUploadById(Guid id);
        Task<FileUpload?> GetFileUploadByCondition(Expression<Func<FileUpload, bool>> expression);

        Task<FileUpload> AddFileUpload(FileUpload fileUpload);
        Task<FileUpload> UpdateFileUpload(FileUpload fileUpload);
        Task DeleteFileUpload(Guid id);
        Task DeleteFileUpload(FileUpload fileUpload);
        Task<VideoEnum.FileUploadStatus> UploadChunk(FileUpload fileUpload, Stream chunkFileStream);

    }
}
