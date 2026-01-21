using System.Linq.Expressions;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace EzioHost.Core.Services.Implement;

public class FileUploadService(
    IFileUploadRepository fileUploadRepository,
    IDirectoryProvider directoryProvider,
    IVideoService videoService,
    IVideoSubtitleService videoSubtitleService,
    ILogger<FileUploadService> logger) : IFileUploadService
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
        logger.LogDebug("Getting file upload by ID: {FileUploadId}", id);
        return fileUploadRepository.GetFileUploadById(id);
    }

    public Task<FileUpload?> GetFileUploadByCondition(Expression<Func<FileUpload, bool>> expression)
    {
        logger.LogDebug("Getting file upload by condition");
        return fileUploadRepository.GetFileUploadByCondition(expression);
    }

    public async Task<FileUpload> AddFileUpload(FileUpload fileUpload)
    {
        logger.LogInformation(
            "Adding new file upload. FileUploadId: {FileUploadId}, FileName: {FileName}, FileSize: {FileSize}, CreatedBy: {CreatedBy}",
            fileUpload.Id,
            fileUpload.FileName,
            fileUpload.FileSize,
            fileUpload.CreatedBy);

        var result = await fileUploadRepository.AddFileUpload(fileUpload);
        logger.LogInformation("Successfully added file upload {FileUploadId}", fileUpload.Id);
        return result;
    }

    public async Task<FileUpload> UpdateFileUpload(FileUpload fileUpload)
    {
        logger.LogDebug(
            "Updating file upload. FileUploadId: {FileUploadId}, Status: {Status}, ReceivedBytes: {ReceivedBytes}/{FileSize}",
            fileUpload.Id,
            fileUpload.Status,
            fileUpload.ReceivedBytes,
            fileUpload.FileSize);

        var result = await fileUploadRepository.UpdateFileUpload(fileUpload);
        return result;
    }

    public async Task DeleteFileUpload(Guid id)
    {
        logger.LogInformation("Deleting file upload. FileUploadId: {FileUploadId}", id);

        try
        {
            await fileUploadRepository.DeleteFileUpload(id);
            var uploadDirectory = GetFileUploadDirectory(id);
            if (Directory.Exists(uploadDirectory))
            {
                Directory.Delete(uploadDirectory, true);
                logger.LogDebug("Deleted upload directory: {Directory}", uploadDirectory);
            }

            logger.LogInformation("Successfully deleted file upload {FileUploadId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting file upload {FileUploadId}", id);
            throw;
        }
    }

    public async Task DeleteFileUpload(FileUpload fileUpload)
    {
        logger.LogInformation(
            "Deleting file upload. FileUploadId: {FileUploadId}, FileName: {FileName}",
            fileUpload.Id,
            fileUpload.FileName);

        try
        {
            await fileUploadRepository.DeleteFileUpload(fileUpload);
            var uploadDirectory = GetFileUploadDirectory(fileUpload.Id);
            if (Directory.Exists(uploadDirectory))
            {
                Directory.Delete(uploadDirectory, true);
                logger.LogDebug("Deleted upload directory: {Directory}", uploadDirectory);
            }

            logger.LogInformation("Successfully deleted file upload {FileUploadId}", fileUpload.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting file upload {FileUploadId}", fileUpload.Id);
            throw;
        }
    }

    public async Task<VideoEnum.FileUploadStatus> UploadChunk(FileUpload fileUpload, Stream chunkFileStream)
    {
        logger.LogDebug(
            "Uploading chunk. FileUploadId: {FileUploadId}, CurrentReceivedBytes: {CurrentReceivedBytes}/{FileSize}",
            fileUpload.Id,
            fileUpload.ReceivedBytes,
            fileUpload.FileSize);

        var uploadDirectory = GetFileUploadDirectory(fileUpload.Id);
        if (!Directory.Exists(uploadDirectory))
        {
            Directory.CreateDirectory(uploadDirectory);
            logger.LogDebug("Created upload directory: {Directory}", uploadDirectory);
        }

        var tempFilePath = GetFileUploadTempPath(fileUpload.Id);

        long chunkSize;
        await using (var stream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            var positionBefore = stream.Position;
            await chunkFileStream.CopyToAsync(stream);
            chunkSize = stream.Position - positionBefore;
        }

        logger.LogDebug(
            "Chunk written to temp file. FileUploadId: {FileUploadId}, ChunkSize: {ChunkSize} bytes",
            fileUpload.Id,
            chunkSize);

        var fileInfo = new FileInfo(tempFilePath);

        if (fileUpload.FileSize < fileInfo.Length)
        {
            logger.LogError(
                "File size exceeded expected size. FileUploadId: {FileUploadId}, Expected: {ExpectedSize}, Actual: {ActualSize}",
                fileUpload.Id,
                fileUpload.FileSize,
                fileInfo.Length);
            throw new InvalidOperationException(
                $"Kích thước file tải lên vượt quá kích thước dự kiến. Dự kiến: {fileUpload.FileSize}, Đã tải lên: {fileInfo.Length}");
        }

        //update received bytes
        var previousReceivedBytes = fileUpload.ReceivedBytes;
        fileUpload.ReceivedBytes = fileInfo.Length;
        var progressPercentage = fileUpload.FileSize > 0
            ? (fileUpload.ReceivedBytes * 100.0 / fileUpload.FileSize)
            : 0;

        logger.LogDebug(
            "Upload progress. FileUploadId: {FileUploadId}, Progress: {ProgressPercentage:F1}% ({ReceivedBytes}/{FileSize} bytes)",
            fileUpload.Id,
            progressPercentage,
            fileUpload.ReceivedBytes,
            fileUpload.FileSize);

        if (fileUpload.FileSize > fileInfo.Length)
        {
            fileUpload.Status = VideoEnum.FileUploadStatus.InProgress;
            await fileUploadRepository.UpdateFileUpload(fileUpload);

            logger.LogInformation(
                "Chunk upload in progress. FileUploadId: {FileUploadId}, Progress: {ProgressPercentage:F1}%",
                fileUpload.Id,
                progressPercentage);

            return VideoEnum.FileUploadStatus.InProgress;
        }

        logger.LogInformation(
            "File upload completed. FileUploadId: {FileUploadId}, TotalSize: {FileSize} bytes",
            fileUpload.Id,
            fileInfo.Length);

        var videoDirectory = Path.Combine(BaseVideoFolder, fileUpload.Id.ToString());

        if (!Directory.Exists(videoDirectory))
        {
            Directory.CreateDirectory(videoDirectory);
            logger.LogDebug("Created video directory: {Directory}", videoDirectory);
        }

        var fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) +
                       Path.GetExtension(fileUpload.FileName);
        var videoFinalPath = Path.Combine(videoDirectory, fileName);

        File.Move(tempFilePath, videoFinalPath, true);
        logger.LogDebug(
            "Moved uploaded file to final location. FileUploadId: {FileUploadId}, FinalPath: {FinalPath}",
            fileUpload.Id,
            videoFinalPath);

        fileUpload.Status = VideoEnum.FileUploadStatus.Completed;
        fileUpload.PhysicalPath = Path.GetRelativePath(BaseWebRootFolder, videoFinalPath);
        await UpdateFileUpload(fileUpload);

        //insert video info to database
        logger.LogInformation(
            "Creating video entity from uploaded file. FileUploadId: {FileUploadId}, FileName: {FileName}",
            fileUpload.Id,
            fileUpload.FileName);

        var newVideo = new Video
        {
            Id = Guid.NewGuid(),
            Title = fileUpload.FileName,
            RawLocation = Path.GetRelativePath(BaseWebRootFolder, videoFinalPath),
            M3U8Location = Path.ChangeExtension(Path.GetRelativePath(BaseWebRootFolder, videoFinalPath), ".m3u8"),
            CreatedBy = fileUpload.CreatedBy,
            Status = VideoEnum.VideoStatus.Queue
        };

        await videoService.AddNewVideo(newVideo);

        logger.LogInformation(
            "Video entity created. FileUploadId: {FileUploadId}, VideoId: {VideoId}",
            fileUpload.Id,
            newVideo.Id);

        // Try to auto-extract embedded subtitles (e.g. from MKV) and store them as WebVTT subtitles
        logger.LogDebug(
            "Attempting to extract embedded subtitles. VideoId: {VideoId}, FilePath: {FilePath}",
            newVideo.Id,
            videoFinalPath);
        await TryExtractEmbeddedSubtitleAsync(newVideo, videoFinalPath, fileUpload.CreatedBy);

        logger.LogInformation(
            "File upload and processing completed successfully. FileUploadId: {FileUploadId}, VideoId: {VideoId}",
            fileUpload.Id,
            newVideo.Id);

        return VideoEnum.FileUploadStatus.Completed;
    }

    public async Task<FileUpload> CopyCompletedFile(FileUpload existingUpload, Guid userId)
    {
        logger.LogInformation(
            "Copying completed file. ExistingFileUploadId: {ExistingFileUploadId}, FileName: {FileName}, UserId: {UserId}",
            existingUpload.Id,
            existingUpload.FileName,
            userId);

        if (string.IsNullOrEmpty(existingUpload.PhysicalPath))
        {
            logger.LogError(
                "Cannot copy file upload {FileUploadId}: PhysicalPath is empty",
                existingUpload.Id);
            throw new InvalidOperationException($"FileUpload {existingUpload.Id} không có PhysicalPath");
        }

        var sourceFile = Path.Combine(BaseWebRootFolder, existingUpload.PhysicalPath);

        if (!File.Exists(sourceFile))
        {
            logger.LogError(
                "Source file not found for copy. FileUploadId: {FileUploadId}, SourcePath: {SourcePath}",
                existingUpload.Id,
                sourceFile);
            throw new InvalidOperationException($"Không tìm thấy file video tại {sourceFile}");
        }

        var sourceFileInfo = new FileInfo(sourceFile);
        logger.LogDebug(
            "Source file found. FileUploadId: {FileUploadId}, Size: {Size} bytes",
            existingUpload.Id,
            sourceFileInfo.Length);

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
        logger.LogDebug("Created new file upload record for copy. NewFileUploadId: {NewFileUploadId}", copyFileUpload.Id);

        var newVideoDirectory = Path.Combine(BaseVideoFolder, copyFileUpload.Id.ToString());
        Directory.CreateDirectory(newVideoDirectory);

        var extension = Path.GetExtension(sourceFile);
        var newFileNameWithoutExt = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        var destinationFile = Path.Combine(newVideoDirectory, $"{newFileNameWithoutExt}{extension}");

        File.Copy(sourceFile, destinationFile, true);
        logger.LogDebug(
            "File copied. SourceFileUploadId: {SourceFileUploadId}, NewFileUploadId: {NewFileUploadId}, Destination: {Destination}",
            existingUpload.Id,
            copyFileUpload.Id,
            destinationFile);

        copyFileUpload.PhysicalPath = Path.GetRelativePath(BaseWebRootFolder, destinationFile);
        await fileUploadRepository.UpdateFileUpload(copyFileUpload);

        var newVideo = new Video
        {
            Id = Guid.NewGuid(),
            Title = existingUpload.FileName,
            RawLocation = Path.GetRelativePath(BaseWebRootFolder, destinationFile),
            M3U8Location = Path.ChangeExtension(
                Path.GetRelativePath(BaseWebRootFolder, destinationFile), ".m3u8"),
            CreatedBy = userId,
            Status = VideoEnum.VideoStatus.Queue
        };

        await videoService.AddNewVideo(newVideo);

        logger.LogInformation(
            "File copy completed successfully. SourceFileUploadId: {SourceFileUploadId}, NewFileUploadId: {NewFileUploadId}, VideoId: {VideoId}",
            existingUpload.Id,
            copyFileUpload.Id,
            newVideo.Id);

        return copyFileUpload;
    }

    private async Task TryExtractEmbeddedSubtitleAsync(Video video, string videoAbsolutePath, Guid userId)
    {
        // This is a best-effort operation: if the file has no subtitle stream or FFmpeg is not available,
        // we silently ignore the error and keep the upload flow successful.
        try
        {
            logger.LogDebug(
                "Analyzing video for embedded subtitles. VideoId: {VideoId}, FilePath: {FilePath}",
                video.Id,
                videoAbsolutePath);

            // Analyze the video file to get all subtitle streams
            var mediaInfo = await FFProbe.AnalyseAsync(videoAbsolutePath);

            // Get all subtitle streams
            var subtitleStreams = mediaInfo.SubtitleStreams.ToList();
            if (!subtitleStreams.Any())
            {
                logger.LogDebug("No embedded subtitle streams found in video {VideoId}", video.Id);
                return;
            }

            logger.LogInformation(
                "Found {Count} embedded subtitle streams in video {VideoId}",
                subtitleStreams.Count,
                video.Id);

            var extractedCount = 0;
            var failedCount = 0;

            for (var subtitleIndex = 0; subtitleIndex < subtitleStreams.Count; subtitleIndex++)
            {
                try
                {
                    var subtitleStream = subtitleStreams[subtitleIndex];
                    var subtitleStreamFFmpegMap = $"-map 0:s:{subtitleIndex}";
                    // Determine language name from stream metadata
                    // Priority: Title > Language > Default name
                    var tags = subtitleStream.Tags;
                    var languageName = tags != null && tags.TryGetValue("title", out var title) && !string.IsNullOrWhiteSpace(title)
                        ? title
                        : tags != null && tags.TryGetValue("language", out var language) && !string.IsNullOrWhiteSpace(language)
                            ? language
                            : $"Subtitle {subtitleIndex}";

                    logger.LogDebug(
                        "Extracting subtitle stream {Index}. VideoId: {VideoId}, Language: {Language}",
                        subtitleIndex,
                        video.Id,
                        languageName);

                    // Create a temporary VTT file for this subtitle stream
                    var tempSubtitlePath = Path.Combine(BaseUploadFolder, $"{Guid.NewGuid()}.vtt");

                    // Extract and convert this specific subtitle stream to WebVTT
                    var arguments = FFMpegArguments
                        .FromFileInput(videoAbsolutePath)
                        .OutputToFile(tempSubtitlePath, true,
                            options => options
                                // Map the specific subtitle stream by its subtitle-stream index
                                .WithCustomArgument(subtitleStreamFFmpegMap)
                                // Ensure output is WebVTT so it can be consumed by the existing subtitle pipeline
                                .WithCustomArgument("-c:s webvtt"));

                    await arguments.ProcessAsynchronously();

                    if (!File.Exists(tempSubtitlePath))
                    {
                        logger.LogWarning(
                            "Subtitle extraction completed but output file not found. VideoId: {VideoId}, StreamIndex: {Index}",
                            video.Id,
                            subtitleIndex);
                        failedCount++;
                        continue;
                    }

                    await using var subtitleFileStream = new FileStream(tempSubtitlePath, FileMode.Open, FileAccess.Read);
                    var fileInfo = new FileInfo(tempSubtitlePath);

                    logger.LogDebug(
                        "Extracted subtitle file. VideoId: {VideoId}, Language: {Language}, Size: {Size} bytes",
                        video.Id,
                        languageName,
                        fileInfo.Length);

                    // Upload the subtitle using the existing pipeline
                    await videoSubtitleService.UploadSubtitleAsync(
                        video.Id,
                        languageName,
                        subtitleFileStream,
                        Path.GetFileName(tempSubtitlePath),
                        fileInfo.Length,
                        userId);

                    extractedCount++;
                    logger.LogInformation(
                        "Successfully extracted and uploaded embedded subtitle. VideoId: {VideoId}, Language: {Language}",
                        video.Id,
                        languageName);

                    // Clean up temporary file
                    try
                    {
                        File.Delete(tempSubtitlePath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Failed to delete temporary subtitle file. VideoId: {VideoId}, Path: {Path}",
                            video.Id,
                            tempSubtitlePath);
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogWarning(ex,
                        "Error extracting subtitle stream {Index} from video {VideoId}. Continuing with next stream.",
                        subtitleIndex,
                        video.Id);
                    // Ignore errors for individual streams, continue with next stream
                    continue;
                }
            }

            logger.LogInformation(
                "Subtitle extraction completed. VideoId: {VideoId}, Extracted: {ExtractedCount}, Failed: {FailedCount}, Total: {TotalCount}",
                video.Id,
                extractedCount,
                failedCount,
                subtitleStreams.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Error during subtitle extraction for video {VideoId}. Upload will continue.",
                video.Id);
            // Ignore any errors during subtitle extraction so that upload is not affected
        }
    }
}