using System.Drawing;
using System.Linq.Expressions;
using AutoMapper;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Domain.Entities;
using EzioHost.Domain.Settings;
using EzioHost.Shared.Events;
using EzioHost.Shared.Extensions;
using EzioHost.Shared.Models;
using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;
using static EzioHost.Shared.Enums.VideoEnum;
using VideoStream = EzioHost.Domain.Entities.VideoStream;

namespace EzioHost.Core.Services.Implement;

public class VideoService(
    IVideoUnitOfWork videoUnitOfWork,
    IDirectoryProvider directoryProvider,
    IProtectService protectService,
    ISettingProvider settingProvider,
    IMapper mapper,
    IM3U8PlaylistService m3U8PlaylistService,
    IVideoResolutionService videoResolutionService,
    IStorageService storageService,
    IEncodingQualitySettingService encodingQualitySettingService,
    ICloudflareStorageSettingService cloudflareStorageSettingService,
    ILogger<VideoService> logger) : IVideoService
{
    private readonly string _thumbnailPath = directoryProvider.GetThumbnailFolder();
    private readonly IVideoRepository _videoRepository = videoUnitOfWork.VideoRepository;
    private readonly IVideoStreamRepository _videoStreamRepository = videoUnitOfWork.VideoStreamRepository;

    private readonly string _webRootPath = directoryProvider.GetWebRootPath();
    private VideoEncodeSettings VideoEncodeSetting => settingProvider.GetVideoEncodeSettings();

    public event EventHandler<VideoStreamAddedEventArgs>? OnVideoStreamAdded;
    public event EventHandler<VideoProcessDoneEvent>? OnVideoProcessDone;

    public Task<Video?> GetVideoWithReadyUpscale(Guid videoId)
    {
        return _videoRepository.GetVideoWithReadyUpscale(videoId);
    }

    public Task<Video?> GetVideoToEncode()
    {
        return _videoRepository.GetVideoToEncode();
    }


    public Task<IEnumerable<Video>> GetVideos(
        int pageNumber,
        int pageSize,
        Expression<Func<Video, bool>>? expression = null,
        Expression<Func<Video, object>>[]? includes = null)
    {
        logger.LogDebug(
            "Getting videos. PageNumber: {PageNumber}, PageSize: {PageSize}, HasFilter: {HasFilter}, IncludesCount: {IncludesCount}",
            pageNumber,
            pageSize,
            expression != null,
            includes?.Length ?? 0);

        return _videoRepository.GetVideos(pageNumber, pageSize, expression, includes);
    }

    public async Task<Video> AddNewVideo(Video newVideo)
    {
        logger.LogInformation(
            "Adding new video. VideoId: {VideoId}, Title: {Title}, CreatedBy: {CreatedBy}",
            newVideo.Id,
            newVideo.Title,
            newVideo.CreatedBy);

        try
        {
            await UpdateResolution(newVideo);
            logger.LogDebug("Updated resolution for video {VideoId}. Resolution: {Resolution}", newVideo.Id, newVideo.Resolution.GetDescription());

            newVideo.Thumbnail = await GenerateThumbnail(newVideo);
            logger.LogDebug("Generated thumbnail for video {VideoId}. Thumbnail: {Thumbnail}", newVideo.Id, newVideo.Thumbnail);

            newVideo.ShareType = VideoShareType.Public;
            await _videoRepository.AddNewVideo(newVideo);

            logger.LogInformation("Successfully added new video {VideoId}", newVideo.Id);
            return newVideo;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding new video {VideoId}", newVideo.Id);
            throw;
        }
    }

    public Task<Video?> GetVideoById(Guid videoId)
    {
        logger.LogDebug("Getting video by ID: {VideoId}", videoId);
        return _videoRepository.GetVideoById(videoId);
    }

    public async Task<Video> UpdateVideo(Video updateVideo)
    {
        logger.LogInformation(
            "Updating video. VideoId: {VideoId}, Title: {Title}, ModifiedBy: {ModifiedBy}",
            updateVideo.Id,
            updateVideo.Title,
            updateVideo.ModifiedBy);

        try
        {
            if (string.IsNullOrEmpty(updateVideo.Thumbnail))
            {
                logger.LogDebug("Thumbnail missing for video {VideoId}, generating new thumbnail", updateVideo.Id);
                await UpdateResolution(updateVideo);
                updateVideo.Thumbnail = await GenerateThumbnail(updateVideo);
            }

            var video = await _videoRepository.UpdateVideo(updateVideo);

            logger.LogInformation("Successfully updated video {VideoId}", updateVideo.Id);
            return video;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating video {VideoId}", updateVideo.Id);
            throw;
        }
    }

    public async Task EncodeVideo(Video inputVideo)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var absoluteRawLocation = Path.Combine(_webRootPath, inputVideo.RawLocation);
        var absoluteM3U8Location = Path.Combine(_webRootPath, inputVideo.M3U8Location);

        logger.LogInformation(
            "Starting video encoding. VideoId: {VideoId}, Resolution: {Resolution}, CreatedBy: {CreatedBy}",
            inputVideo.Id,
            inputVideo.Resolution,
            inputVideo.CreatedBy);

        try
        {
            await videoUnitOfWork.BeginTransactionAsync();
            logger.LogDebug("Transaction started for video encoding {VideoId}", inputVideo.Id);

            inputVideo.VideoStreams ??= [];

            // Get user's Cloudflare storage setting and set flag
            var cloudflareSetting = await cloudflareStorageSettingService.GetUserSettingsAsync(inputVideo.CreatedBy);
            inputVideo.IsCloudflareEnabled = cloudflareSetting.IsEnabled;
            logger.LogDebug(
                "Cloudflare storage setting for video {VideoId}: IsEnabled={IsEnabled}",
                inputVideo.Id,
                cloudflareSetting.IsEnabled);

            // Get user's active encoding settings
            var userSettings = await encodingQualitySettingService.GetActiveSettingsForEncoding(inputVideo.CreatedBy);
            var enabledResolutions = userSettings.Select(s => s.Resolution).ToHashSet();
            logger.LogDebug(
                "User {UserId} has {Count} enabled resolutions for encoding",
                inputVideo.CreatedBy,
                enabledResolutions.Count);

            // Get existing resolutions that are already encoded
            var existingResolutions = inputVideo.VideoStreams
                .Select(vs => vs.Resolution)
                .ToHashSet();

            // Filter resolutions: only encode enabled resolutions that are <= video resolution and not already encoded
            var resolutionsToEncode = inputVideo.Resolution.GetEnumsLessThanOrEqual()
                .Where(r => enabledResolutions.Contains(r) && !existingResolutions.Contains(r))
                .ToList();

            // If no enabled resolutions match (e.g., video is 480p but user only enabled 720p, 1080p),
            // encode the original video resolution to ensure at least one version is available
            if (resolutionsToEncode.Count == 0 && !existingResolutions.Contains(inputVideo.Resolution))
            {
                logger.LogInformation(
                    "No enabled resolutions match video resolution {VideoResolution}. Encoding original resolution to ensure playback availability.",
                    inputVideo.Resolution);
                resolutionsToEncode.Add(inputVideo.Resolution);
            }

            // Ensure at least one resolution is encoded
            if (resolutionsToEncode.Count == 0 && !inputVideo.VideoStreams.Any())
            {
                logger.LogError(
                    "Cannot encode video {VideoId}: No resolutions available for encoding. User must enable at least one resolution in settings.",
                    inputVideo.Id);
                throw new InvalidOperationException(
                    $"Cannot encode video {inputVideo.Id}: No resolutions available for encoding. Please enable at least one resolution in settings.");
            }

            logger.LogInformation(
                "Encoding {Count} resolutions for video {VideoId}: {Resolutions}",
                resolutionsToEncode.Count,
                inputVideo.Id,
                string.Join(", ", resolutionsToEncode));

            var resolutionIndex = 0;
            foreach (var videoResolution in resolutionsToEncode)
            {
                resolutionIndex++;
                logger.LogInformation(
                    "Encoding resolution {Resolution} ({Current}/{Total}) for video {VideoId}",
                    videoResolution,
                    resolutionIndex,
                    resolutionsToEncode.Count,
                    inputVideo.Id);

                var newVideoStream = await CreateHlsVariantStream(absoluteRawLocation, inputVideo, videoResolution,
                    inputVideo.CreatedBy);

                await AddNewVideoStream(inputVideo, newVideoStream);

                logger.LogInformation(
                    "Successfully encoded resolution {Resolution} for video {VideoId}",
                    videoResolution,
                    inputVideo.Id);
            }

            logger.LogDebug("Building full M3U8 playlist for video {VideoId}", inputVideo.Id);
            await m3U8PlaylistService.BuildFullPlaylistAsync(inputVideo, absoluteM3U8Location);

            inputVideo.Status = VideoStatus.Ready;

            await videoUnitOfWork.VideoRepository.UpdateVideoForUnitOfWork(inputVideo);

            await videoUnitOfWork.CommitTransactionAsync();
            logger.LogDebug("Transaction committed for video encoding {VideoId}", inputVideo.Id);

            stopwatch.Stop();
            logger.LogInformation(
                "Video encoding completed successfully. VideoId: {VideoId}, Duration: {DurationMs}ms, ResolutionsEncoded: {ResolutionsCount}",
                inputVideo.Id,
                stopwatch.ElapsedMilliseconds,
                resolutionsToEncode.Count);

            var videoMapper = mapper.Map<VideoDto>(inputVideo);

            OnVideoProcessDone?.Invoke(this, new VideoProcessDoneEvent
            {
                Video = videoMapper
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Error encoding video {VideoId} after {DurationMs}ms. Rolling back transaction.",
                inputVideo.Id,
                stopwatch.ElapsedMilliseconds);
            await videoUnitOfWork.RollbackTransactionAsync();
            logger.LogDebug("Transaction rolled back for video encoding {VideoId}", inputVideo.Id);
            throw;
        }
    }


    public async Task<VideoStream> AddNewVideoStream(Video video, VideoStream videoStream)
    {
        logger.LogInformation(
            "Adding new video stream. VideoId: {VideoId}, VideoStreamId: {VideoStreamId}, Resolution: {Resolution}",
            video.Id,
            videoStream.Id,
            videoStream.Resolution);

        video.VideoStreams ??= [];

        // Check if this resolution already exists in the collection to avoid duplicates
        var existingStream = video.VideoStreams.FirstOrDefault(vs => vs.Resolution == videoStream.Resolution);
        if (existingStream != null)
        {
            logger.LogWarning(
                "VideoStream with resolution {Resolution} already exists for video {VideoId}. Skipping duplicate.",
                videoStream.Resolution, video.Id);
            return existingStream;
        }

        _videoStreamRepository.Add(videoStream);
        videoStream.Video = video;

        logger.LogDebug("Video stream {VideoStreamId} added to repository", videoStream.Id);

        OnVideoStreamAdded?.Invoke(this, new VideoStreamAddedEventArgs
        {
            VideoId = video.Id,
            VideoStream = mapper.Map<VideoStreamDto>(videoStream)
        });

        await UploadSegmentsToStorageAsync(videoStream);

        logger.LogInformation(
            "Successfully added video stream {VideoStreamId} for video {VideoId}",
            videoStream.Id,
            video.Id);

        return videoStream;
    }

    public async Task<VideoStream> CreateHlsVariantStream(string absoluteRawLocation, Video inputVideo,
        VideoResolution targetResolution, Guid userId, int scale = 2)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation(
            "Creating HLS variant stream. VideoId: {VideoId}, Resolution: {Resolution}, UserId: {UserId}",
            inputVideo.Id,
            targetResolution,
            userId);

        var segmentFolder = Path.Combine(_webRootPath, Path.GetDirectoryName(inputVideo.M3U8Location)!,
            targetResolution.GetDescription());
        if (!Directory.Exists(segmentFolder))
        {
            Directory.CreateDirectory(segmentFolder);
            logger.LogDebug("Created segment folder: {Folder}", segmentFolder);
        }

        var segmentPath = Path.Combine(segmentFolder, $"{targetResolution.GetDescription()}_%07d.ts");
        var absoluteVideoStreamM3U8Location = Path.Combine(segmentFolder, $"{targetResolution.GetDescription()}.m3u8");

        var videoStream = new VideoStream
        {
            Id = Guid.NewGuid(),
            Resolution = targetResolution,
            VideoId = inputVideo.Id,
            Video = inputVideo,
            Key = protectService.GenerateRandomKey(), // 🔑 Random key
            Iv = protectService.GenerateRandomIv(), // 🔄 Random IV
            M3U8Location = Path.GetRelativePath(_webRootPath, absoluteVideoStreamM3U8Location)
        };

        logger.LogDebug(
            "Created video stream entity. VideoStreamId: {VideoStreamId}, M3U8Location: {M3U8Location}",
            videoStream.Id,
            videoStream.M3U8Location);

        var videoInput = await FFProbe.AnalyseAsync(absoluteRawLocation);

        var videoInputStream = videoInput.PrimaryVideoStream;
        if (videoInputStream == null)
        {
            logger.LogError("No video stream found in input file for video {VideoId}", inputVideo.Id);
            throw new InvalidOperationException("No video stream found in the input file.");
        }

        var videoRatio = 1.0 * videoInputStream.DisplayAspectRatio.Width / videoInputStream.DisplayAspectRatio.Height;
        var targetHeight = (int)targetResolution;
        if (targetResolution == VideoResolution.Upscaled) targetHeight = videoInputStream.Height;
        var targetWidth = (int)Math.Round(targetHeight * videoRatio);

        if (targetWidth % 2 != 0) targetWidth++;
        if (targetResolution == VideoResolution.Upscaled) targetWidth = videoInputStream.Width;

        var resolutionSize = new Size(targetWidth, targetHeight);

        logger.LogDebug(
            "Calculated target resolution. VideoId: {VideoId}, Original: {OriginalWidth}x{OriginalHeight}, Target: {TargetWidth}x{TargetHeight}",
            inputVideo.Id,
            videoInputStream.Width,
            videoInputStream.Height,
            resolutionSize.Width,
            resolutionSize.Height);

        // Get bitrate from user settings
        var bitrateBps =
            await videoResolutionService.GetBandwidthForResolutionAsync(targetResolution.GetDescription(), userId);
        var bitrateKbps = bitrateBps / 1000;

        logger.LogDebug(
            "Using bitrate for encoding. VideoId: {VideoId}, Resolution: {Resolution}, Bitrate: {BitrateKbps}kbps",
            inputVideo.Id,
            targetResolution,
            bitrateKbps);

        var argumentProcessor = FFMpegArguments
            .FromFileInput(absoluteRawLocation)
            .OutputToFile(absoluteVideoStreamM3U8Location, true,
                options => options
                    .WithVideoCodec(VideoEncodeSetting.VideoCodec)
                    .WithAudioCodec(VideoEncodeSetting.AudioCodec)
                    .WithVideoBitrate(bitrateKbps) //kbps
                    .WithAudioBitrate(AudioQuality.Normal) //128kbps
                    .WithCustomArgument(
                        $"-vf \"scale={resolutionSize.Width}:{resolutionSize.Height},format=yuv420p\"") //handle 10 bit file
                    .WithCustomArgument("-force_key_frames \"expr:gte(t,n_forced*1)\"")
                    .WithCustomArgument("-f hls")
                    .WithCustomArgument($"-hls_time {VideoEncodeSetting.HlsTime}")
                    .WithCustomArgument($"-hls_segment_filename \"{segmentPath}\"")
                    .WithCustomArgument("-hls_playlist_type vod")
                    .WithCustomArgument("-hls_enc 1")
                    .WithCustomArgument($"-hls_enc_key \"{videoStream.Key}\"")
                    .WithCustomArgument($"-hls_enc_iv \"{videoStream.Iv}\"")
                    .WithFastStart()
            );

        try
        {
            logger.LogInformation(
                "Starting FFMpeg encoding for video {VideoId}, resolution {Resolution}",
                inputVideo.Id,
                targetResolution);

            await argumentProcessor.ProcessAsynchronously();

            stopwatch.Stop();
            logger.LogInformation(
                "FFMpeg encoding completed. VideoId: {VideoId}, Resolution: {Resolution}, Duration: {DurationMs}ms",
                inputVideo.Id,
                targetResolution,
                stopwatch.ElapsedMilliseconds);

            var m3U8Content = await File.ReadAllLinesAsync(absoluteVideoStreamM3U8Location);

            for (var i = 0; i < m3U8Content.Length; i++)
                if (m3U8Content[i].StartsWith("#EXT-X-KEY:"))
                {
                    var line = m3U8Content[i];
                    var startIndex = line.IndexOf("URI=\"", StringComparison.Ordinal);
                    if (startIndex != -1)
                    {
                        startIndex += "URI=\"".Length;
                        var endIndex = line.IndexOf("\"", startIndex, StringComparison.Ordinal);
                        if (endIndex != -1)
                        {
                            var oldUri = line.Substring(startIndex, endIndex - startIndex);

                            var newUri = Path.Combine(VideoEncodeSetting.BaseDrmUrl, videoStream.Id.ToString())
                                .Replace("\\", "/");

                            m3U8Content[i] = line.Replace(oldUri, newUri);
                            logger.LogDebug(
                                "Updated DRM URI in M3U8. VideoStreamId: {VideoStreamId}, NewUri: {NewUri}",
                                videoStream.Id,
                                newUri);
                        }
                    }

                    break;
                }

            await File.WriteAllLinesAsync(absoluteVideoStreamM3U8Location, m3U8Content);
            logger.LogDebug("M3U8 playlist file updated. VideoStreamId: {VideoStreamId}", videoStream.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "FFMpeg processing error while creating HLS variant stream for video {VideoId}, resolution {Resolution} after {DurationMs}ms",
                inputVideo.Id,
                targetResolution,
                stopwatch.ElapsedMilliseconds);
            throw;
        }

        logger.LogInformation(
            "Successfully created HLS variant stream. VideoStreamId: {VideoStreamId}, VideoId: {VideoId}, Resolution: {Resolution}",
            videoStream.Id,
            inputVideo.Id,
            targetResolution);

        return videoStream;
    }

    public async Task DeleteVideo(Video deleteVideo)
    {
        logger.LogInformation(
            "Deleting video. VideoId: {VideoId}, Title: {Title}",
            deleteVideo.Id,
            deleteVideo.Title);

        try
        {
            //var videoLocation = Path.Combine(_webRootPath, deleteVideo.RawLocation);
            //Directory.Delete(Path.GetDirectoryName(videoLocation) ?? throw new InvalidOperationException());
            await _videoRepository.DeleteVideo(deleteVideo);

            logger.LogInformation("Successfully deleted video {VideoId}", deleteVideo.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting video {VideoId}", deleteVideo.Id);
            throw;
        }
    }

    public Task<Video?> GetVideoByVideoStreamId(Guid videoStreamId)
    {
        logger.LogDebug("Getting video by video stream ID: {VideoStreamId}", videoStreamId);
        return _videoRepository.GetVideoByVideoStreamId(videoStreamId);
    }

    public async Task<string> GenerateThumbnail(Video video)
    {
        logger.LogInformation("Generating thumbnail for video {VideoId}", video.Id);

        try
        {
            var rawLocation = Path.Combine(_webRootPath, video.RawLocation);
            var upscaleLocation = Path.Combine(_webRootPath,
                video.VideoUpscales.FirstOrDefault()?.OutputLocation ?? string.Empty);

            if (!string.IsNullOrEmpty(upscaleLocation) && File.Exists(upscaleLocation))
            {
                rawLocation = upscaleLocation;
                logger.LogDebug("Using upscaled video for thumbnail generation. VideoId: {VideoId}", video.Id);
            }

            var mediaInfo = await FFProbe.AnalyseAsync(rawLocation);
            var duration = mediaInfo.Duration;

            var random = new Random();
            var randomSeconds = random.NextDouble() * duration.TotalSeconds;
            var captureTime = TimeSpan.FromSeconds(randomSeconds);

            logger.LogDebug(
                "Thumbnail capture time calculated. VideoId: {VideoId}, Duration: {Duration}s, CaptureTime: {CaptureTime}s",
                video.Id,
                duration.TotalSeconds,
                captureTime.TotalSeconds);

            var thumbFileName = $"thumb_{video.Id}.jpg";
            var thumbFullPath = Path.Combine(_thumbnailPath, thumbFileName);

            await FFMpeg.SnapshotAsync(rawLocation, thumbFullPath, null, captureTime);

            var relativePath = Path.GetRelativePath(_webRootPath, thumbFullPath);
            logger.LogInformation(
                "Successfully generated thumbnail for video {VideoId}. Thumbnail: {Thumbnail}",
                video.Id,
                relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating thumbnail for video {VideoId}", video.Id);
            throw;
        }
    }

    public Task<Video?> GetVideoBackup()
    {
        logger.LogDebug("Getting video for backup");
        return _videoRepository.GetVideoToBackup();
    }

    public async Task<VideoBackupStatus> BackupVideo(Video video)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation(
            "Starting video backup. VideoId: {VideoId}, Title: {Title}",
            video.Id,
            video.Title);

        try
        {
            var localVideoPath = Path.Combine(_webRootPath, video.RawLocation);
            var fileName = Path.GetFileName(localVideoPath);
            var key = $"videos/{video.Id}/{fileName}";

            if (!File.Exists(localVideoPath))
            {
                logger.LogWarning(
                    "Video file not found for backup. VideoId: {VideoId}, Path: {Path}",
                    video.Id,
                    localVideoPath);
                throw new FileNotFoundException($"Video file not found: {localVideoPath}");
            }

            var fileInfo = new FileInfo(localVideoPath);
            logger.LogInformation(
                "Uploading video to storage. VideoId: {VideoId}, Key: {Key}, Size: {Size} bytes",
                video.Id,
                key,
                fileInfo.Length);

            await storageService.UploadLargeFileAsync(localVideoPath, key, "application/octet-stream"); //default

            stopwatch.Stop();
            logger.LogInformation(
                "Video uploaded to storage. VideoId: {VideoId}, Duration: {DurationMs}ms",
                video.Id,
                stopwatch.ElapsedMilliseconds);

            video.BackupStatus = VideoBackupStatus.BackedUp;
            await UpdateVideo(video);

            logger.LogInformation("Video backup completed successfully. VideoId: {VideoId}", video.Id);
            return video.BackupStatus;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Error backing up video {VideoId} after {DurationMs}ms",
                video.Id,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task UpdateResolution(Video video)
    {
        logger.LogDebug("Updating resolution for video {VideoId}", video.Id);

        try
        {
            var rawLocation = Path.Combine(_webRootPath, video.RawLocation);

            var mediaInfo = await FFProbe.AnalyseAsync(rawLocation);
            var videoHeight = mediaInfo.PrimaryVideoStream!.Height;

            video.Height = videoHeight;

            logger.LogDebug(
                "Resolution updated for video {VideoId}. Height: {Height}",
                video.Id,
                videoHeight);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating resolution for video {VideoId}", video.Id);
            throw;
        }
    }

    public async Task UploadSegmentsToStorageAsync(VideoStream videoStream)
    {
        // Check if Cloudflare storage is enabled for this video
        if (videoStream.Video.IsCloudflareEnabled != true)
        {
            logger.LogInformation(
                "Skipping Cloudflare R2 upload for video {VideoId} - Cloudflare storage is disabled",
                videoStream.Video.Id);
            return;
        }

        logger.LogInformation(
            "Starting upload of video segments to storage. VideoId: {VideoId}, VideoStreamId: {VideoStreamId}, Resolution: {Resolution}",
            videoStream.Video.Id,
            videoStream.Id,
            videoStream.Resolution);

        var relativePath = Uri.UnescapeDataString(videoStream.M3U8Location);

        var fullFilePath = Path.Combine(_webRootPath, relativePath);
        var folderPath = Path.GetDirectoryName(fullFilePath)!;

        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning(
                "Segment folder not found. VideoStreamId: {VideoStreamId}, Folder: {Folder}",
                videoStream.Id,
                folderPath);
            return;
        }

        var tsFiles = Directory.GetFiles(folderPath, "*.ts");
        logger.LogInformation(
            "Found {Count} segment files to upload. VideoStreamId: {VideoStreamId}",
            tsFiles.Length,
            videoStream.Id);

        var uploadedCount = 0;
        var failedCount = 0;

        await Parallel.ForEachAsync(tsFiles, async (filePath, ct) =>
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var key = $"videos/{videoStream.Video.Id}/{(int)videoStream.Resolution}/{fileName}";

                var fileInfo = new FileInfo(filePath);
                logger.LogDebug(
                    "Uploading segment. VideoStreamId: {VideoStreamId}, File: {FileName}, Size: {Size} bytes",
                    videoStream.Id,
                    fileName,
                    fileInfo.Length);

                await storageService.UploadFileAsync(filePath, key, "video/MP2T");

                Interlocked.Increment(ref uploadedCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failedCount);
                logger.LogError(ex,
                    "Error uploading segment file. VideoStreamId: {VideoStreamId}, File: {FilePath}",
                    videoStream.Id,
                    filePath);
            }
        });

        logger.LogInformation(
            "Completed upload of video segments. VideoStreamId: {VideoStreamId}, Uploaded: {UploadedCount}, Failed: {FailedCount}, Total: {TotalCount}",
            videoStream.Id,
            uploadedCount,
            failedCount,
            tsFiles.Length);
    }
}