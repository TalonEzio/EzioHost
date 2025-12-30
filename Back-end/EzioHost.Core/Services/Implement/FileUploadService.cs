using System.Linq.Expressions;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;

namespace EzioHost.Core.Services.Implement;

public class FileUploadService(
    IFileUploadRepository fileUploadRepository,
    IDirectoryProvider directoryProvider,
    IVideoService videoService) : IFileUploadService
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
        if (!Directory.Exists(uploadDirectory)) Directory.CreateDirectory(uploadDirectory);
        var tempFilePath = GetFileUploadTempPath(fileUpload.Id);

        await using (var stream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            await chunkFileStream.CopyToAsync(stream);
        }

        var fileInfo = new FileInfo(tempFilePath);

        if (fileUpload.FileSize < fileInfo.Length)
        {
            throw new InvalidOperationException(
                $"Kích thước file tải lên vượt quá kích thước dự kiến. Dự kiến: {fileUpload.FileSize}, Đã tải lên: {fileInfo.Length}");
        }

        //update received bytes
        fileUpload.ReceivedBytes = fileInfo.Length;

        if (fileUpload.FileSize > fileInfo.Length)
        {
            fileUpload.Status = VideoEnum.FileUploadStatus.InProgress;
            await fileUploadRepository.UpdateFileUpload(fileUpload);

            return VideoEnum.FileUploadStatus.InProgress;
        }

        var videoDirectory = Path.Combine(BaseVideoFolder, fileUpload.Id.ToString());

        if (!Directory.Exists(videoDirectory)) Directory.CreateDirectory(videoDirectory);

        var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) +
                       Path.GetExtension(fileUpload.FileName);
        var videoFinalPath = Path.Combine(videoDirectory, fileName);

        File.Move(tempFilePath, videoFinalPath, true);

        fileUpload.Status = VideoEnum.FileUploadStatus.Completed;
        fileUpload.PhysicalPath = Path.GetRelativePath(BaseWebRootFolder, videoFinalPath);
        await UpdateFileUpload(fileUpload);

        //insert video info to database

        var newVideo = new Video
        {
            Title = fileUpload.FileName,
            RawLocation = Path.GetRelativePath(BaseWebRootFolder, videoFinalPath),
            M3U8Location = Path.ChangeExtension(Path.GetRelativePath(BaseWebRootFolder, videoFinalPath), ".m3u8"),
            CreatedBy = fileUpload.CreatedBy,
            Status = VideoEnum.VideoStatus.Queue
        };

        await videoService.AddNewVideo(newVideo);
        return VideoEnum.FileUploadStatus.Completed;
    }

    public async Task<FileUpload> CopyCompletedFile(FileUpload existingUpload, Guid userId)
    {
        if (string.IsNullOrEmpty(existingUpload.PhysicalPath))
            throw new InvalidOperationException($"FileUpload {existingUpload.Id} không có PhysicalPath");

        var sourceFile = Path.Combine(BaseWebRootFolder, existingUpload.PhysicalPath);

        if (!File.Exists(sourceFile))
            throw new InvalidOperationException($"Không tìm thấy file video tại {sourceFile}");

        var copyFileUpload = new FileUpload
        {
            FileName = existingUpload.FileName,
            FileSize = existingUpload.FileSize,
            ReceivedBytes = existingUpload.ReceivedBytes,
            ContentType = existingUpload.ContentType,
            Checksum = existingUpload.Checksum,
            CreatedBy = userId,
            Status = VideoEnum.FileUploadStatus.Completed
        };

        await fileUploadRepository.AddFileUpload(copyFileUpload);

        var newVideoDirectory = Path.Combine(BaseVideoFolder, copyFileUpload.Id.ToString());
        Directory.CreateDirectory(newVideoDirectory);

        var extension = Path.GetExtension(sourceFile);
        var newFileNameWithoutExt = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        var destinationFile = Path.Combine(newVideoDirectory, $"{newFileNameWithoutExt}{extension}");

        File.Copy(sourceFile, destinationFile, true);

        copyFileUpload.PhysicalPath = Path.GetRelativePath(BaseWebRootFolder, destinationFile);
        await fileUploadRepository.UpdateFileUpload(copyFileUpload);

        var newVideo = new Video
        {
            Title = existingUpload.FileName,
            RawLocation = Path.GetRelativePath(BaseWebRootFolder, destinationFile),
            M3U8Location = Path.ChangeExtension(
                Path.GetRelativePath(BaseWebRootFolder, destinationFile), ".m3u8"),
            CreatedBy = userId,
            Status = VideoEnum.VideoStatus.Queue
        };

        await videoService.AddNewVideo(newVideo);

        return copyFileUpload;
    }
}