
namespace EzioHost.Core.Services.Interface
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(string localFilePath, string key, string contentType);

        Task<string> UploadLargeFileAsync(string localFilePath, string key, string contentType);
    }
}
