using System.Linq.Expressions;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;

namespace EzioHost.Core.Services.Implement
{
    public class FileUploadService(IFileUploadRepository fileUploadRepository, IDirectoryProvider directoryProvider,IVideoService videoService) : IFileUploadService
    {
        private string BaseWebRootFolder => directoryProvider.GetWebRootPath();
        private string BaseUploadFolder => directoryProvider.GetBaseUploadFolder();
        private string BaseVideoFolder => directoryProvider.GetBaseVideoFolder();

        public string GetFileUploadDirectory(Guid fileUploadId)
        {
            var uploadDirectory = Path.Combine(BaseUploadFolder, fileUploadId.ToString());
            return uploadDirectory;
        }
        public string GetFileUploadTempPath(Guid fileUploadId)
        {
            var uploadDirectory = GetFileUploadDirectory(fileUploadId);
            var tempFilePath = Path.Combine(uploadDirectory, fileUploadId + ".chunk");
            return tempFilePath;
        }

        public Task<FileUpload?> GetFileUploadById(Guid id)
        {
            return fileUploadRepository.GetFileUploadById(id);
        }

        public Task<FileUpload?> GetFileUploadByCondition(Expression<Func<FileUpload, bool>> expression)
        {
            return fileUploadRepository.GetFileUploadByCondition(expression);
        }

        public Task<FileUpload> AddFileUpload(FileUpload fileUpload)
        {
            return fileUploadRepository.AddFileUpload(fileUpload);
        }

        public Task<FileUpload> UpdateFileUpload(FileUpload fileUpload)
        {
            return fileUploadRepository.UpdateFileUpload(fileUpload);
        }

        public async Task DeleteFileUpload(Guid id)
        {
            await fileUploadRepository.DeleteFileUpload(id);
            Directory.Delete(GetFileUploadDirectory(id));
        }

        public async Task DeleteFileUpload(FileUpload fileUpload)
        {
            await fileUploadRepository.DeleteFileUpload(fileUpload);
            Directory.Delete(GetFileUploadDirectory(fileUpload.Id));
        }

        public async Task<VideoEnum.FileUploadStatus> UploadChunk(FileUpload fileUpload, Stream chunkFileStream)
        {
            var uploadDirectory = GetFileUploadDirectory(fileUpload.Id);
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }
            var tempFilePath = GetFileUploadTempPath(fileUpload.Id);

            await using (var stream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                await chunkFileStream.CopyToAsync(stream);
            }

            var fileInfo = new FileInfo(tempFilePath);

            if (fileInfo.Length < fileUpload.FileSize)
            {
                fileUpload.UploadedBytes = fileInfo.Length;
                fileUpload.Status = VideoEnum.FileUploadStatus.InProgress;
                await fileUploadRepository.UpdateFileUpload(fileUpload);

                return VideoEnum.FileUploadStatus.InProgress;
            }

            var videoDirectory = Path.Combine(BaseVideoFolder, fileUpload.Id.ToString());

            if (!Directory.Exists(videoDirectory))
            {
                Directory.CreateDirectory(videoDirectory);
            }

            var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(fileUpload.FileName);
            var videoFinalPath = Path.Combine(videoDirectory, fileName);

            File.Move(tempFilePath, videoFinalPath, true);

            fileUpload.UploadedBytes = fileInfo.Length;
            fileUpload.Status = VideoEnum.FileUploadStatus.Completed;
            await UpdateFileUpload(fileUpload);

            //insert video info to database

            var newVideo = new Video
            {
                Title = fileUpload.FileName,
                RawLocation = Path.GetRelativePath(BaseWebRootFolder, videoFinalPath),
                M3U8Location = Path.ChangeExtension(Path.GetRelativePath(BaseWebRootFolder, videoFinalPath), ".m3u8"),
                CreatedBy = fileUpload.CreatedBy,
                Status = VideoEnum.VideoStatus.Queue,
                Type = fileUpload.Type
            };
            
            await videoService.AddNewVideo(newVideo);
            await DeleteFileUpload(fileUpload);
            return VideoEnum.FileUploadStatus.Completed;
        }
    }
}
