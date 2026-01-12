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
        return _videoRepository.GetVideos(pageNumber, pageSize, expression, includes);
    }

    public async Task<Video> AddNewVideo(Video newVideo)
    {
        await UpdateResolution(newVideo);
        newVideo.Thumbnail = await GenerateThumbnail(newVideo);
        newVideo.ShareType = VideoShareType.Public;
        await _videoRepository.AddNewVideo(newVideo);

        return newVideo;
    }

    public Task<Video?> GetVideoById(Guid videoId)
    {
        return _videoRepository.GetVideoById(videoId);
    }

    public async Task<Video> UpdateVideo(Video updateVideo)
    {
        if (string.IsNullOrEmpty(updateVideo.Thumbnail))
        {
            await UpdateResolution(updateVideo);
            updateVideo.Thumbnail = await GenerateThumbnail(updateVideo);
        }

        var video = await _videoRepository.UpdateVideo(updateVideo);

        return video;
    }

    public async Task EncodeVideo(Video inputVideo)
    {
        var absoluteRawLocation = Path.Combine(_webRootPath, inputVideo.RawLocation);
        var absoluteM3U8Location = Path.Combine(_webRootPath, inputVideo.M3U8Location);

        try
        {
            await videoUnitOfWork.BeginTransactionAsync();

            inputVideo.VideoStreams ??= [];

            // Get user's active encoding settings
            var userSettings = await encodingQualitySettingService.GetActiveSettingsForEncoding(inputVideo.CreatedBy);
            var enabledResolutions = userSettings.Select(s => s.Resolution).ToHashSet();

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
                throw new InvalidOperationException(
                    $"Cannot encode video {inputVideo.Id}: No resolutions available for encoding. Please enable at least one resolution in settings.");
            }

            foreach (var videoResolution in resolutionsToEncode)
            {
                var newVideoStream = await CreateHlsVariantStream(absoluteRawLocation, inputVideo, videoResolution, inputVideo.CreatedBy);

                await AddNewVideoStream(inputVideo, newVideoStream);
            }

            await m3U8PlaylistService.BuildFullPlaylistAsync(inputVideo, absoluteM3U8Location);

            inputVideo.Status = VideoStatus.Ready;

            await videoUnitOfWork.VideoRepository.UpdateVideoForUnitOfWork(inputVideo);

            await videoUnitOfWork.CommitTransactionAsync();

            var videoMapper = mapper.Map<VideoDto>(inputVideo);

            OnVideoProcessDone?.Invoke(this, new VideoProcessDoneEvent
            {
                Video = videoMapper
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error encoding video {VideoId}. Rolling back transaction.", inputVideo.Id);
            await videoUnitOfWork.RollbackTransactionAsync();
            throw;
        }
    }


    public async Task<VideoStream> AddNewVideoStream(Video video, VideoStream videoStream)
    {
        video.VideoStreams ??= [];
        
        // Check if this resolution already exists in the collection to avoid duplicates
        var existingStream = video.VideoStreams.FirstOrDefault(vs => vs.Resolution == videoStream.Resolution);
        if (existingStream != null)
        {
            logger.LogWarning("VideoStream with resolution {Resolution} already exists for video {VideoId}. Skipping duplicate.", 
                videoStream.Resolution, video.Id);
            return existingStream;
        }

        _videoStreamRepository.Add(videoStream);
        videoStream.Video = video;

        OnVideoStreamAdded?.Invoke(this, new VideoStreamAddedEventArgs
        {
            VideoId = video.Id,
            VideoStream = mapper.Map<VideoStreamDto>(videoStream)
        });

        await UploadSegmentsToStorageAsync(videoStream);
        return videoStream;
    }

    public async Task<VideoStream> CreateHlsVariantStream(string absoluteRawLocation, Video inputVideo,
        VideoResolution targetResolution, Guid userId, int scale = 2)
    {
        var segmentFolder = Path.Combine(_webRootPath, Path.GetDirectoryName(inputVideo.M3U8Location)!,
            targetResolution.GetDescription());
        if (!Directory.Exists(segmentFolder)) Directory.CreateDirectory(segmentFolder);

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

        var videoInput = await FFProbe.AnalyseAsync(absoluteRawLocation);

        var videoInputStream = videoInput.PrimaryVideoStream;
        if (videoInputStream == null)
            throw new InvalidOperationException("No video stream found in the input file.");

        var videoRatio = 1.0 * videoInputStream.DisplayAspectRatio.Width / videoInputStream.DisplayAspectRatio.Height;
        var targetHeight = (int)targetResolution;
        if (targetResolution == VideoResolution.Upscaled) targetHeight = videoInputStream.Height;
        var targetWidth = (int)Math.Round(targetHeight * videoRatio);

        if (targetWidth % 2 != 0) targetWidth++;
        if (targetResolution == VideoResolution.Upscaled) targetWidth = videoInputStream.Width;

        var resolutionSize = new Size(targetWidth, targetHeight);

        // Get bitrate from user settings
        var bitrateBps = await videoResolutionService.GetBandwidthForResolutionAsync(targetResolution.GetDescription(), userId);
        var bitrateKbps = bitrateBps / 1000;

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
            await argumentProcessor.ProcessAsynchronously();

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
                        }
                    }

                    break;
                }

            await File.WriteAllLinesAsync(absoluteVideoStreamM3U8Location, m3U8Content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "FFMpeg processing error while creating HLS variant stream for video {VideoId}, resolution {Resolution}",
                inputVideo.Id, targetResolution);
            throw;
        }

        return videoStream;
    }

    public async Task DeleteVideo(Video deleteVideo)
    {
        //var videoLocation = Path.Combine(_webRootPath, deleteVideo.RawLocation);
        //Directory.Delete(Path.GetDirectoryName(videoLocation) ?? throw new InvalidOperationException());
        await _videoRepository.DeleteVideo(deleteVideo);
    }

    public Task<Video?> GetVideoByVideoStreamId(Guid videoStreamId)
    {
        return _videoRepository.GetVideoByVideoStreamId(videoStreamId);
    }

    public async Task<string> GenerateThumbnail(Video video)
    {
        var rawLocation = Path.Combine(_webRootPath, video.RawLocation);
        var upscaleLocation = Path.Combine(_webRootPath, video.VideoUpscales.FirstOrDefault()?.OutputLocation ?? string.Empty);

        if (!string.IsNullOrEmpty(upscaleLocation) && File.Exists(upscaleLocation))
        {
            rawLocation = upscaleLocation;
        }

        var mediaInfo = await FFProbe.AnalyseAsync(rawLocation);
        var duration = mediaInfo.Duration;

        var random = new Random();
        var randomSeconds = random.NextDouble() * duration.TotalSeconds;
        var captureTime = TimeSpan.FromSeconds(randomSeconds);

        var thumbFileName = $"thumb_{video.Id}.jpg";
        var thumbFullPath = Path.Combine(_thumbnailPath, thumbFileName);

        await FFMpeg.SnapshotAsync(rawLocation, thumbFullPath, null, captureTime);

        return Path.GetRelativePath(_webRootPath, thumbFullPath);
    }

    public Task<Video?> GetVideoBackup()
    {
        return _videoRepository.GetVideoToBackup();
    }

    public async Task<VideoBackupStatus> BackupVideo(Video video)
    {
        var localVideoPath = Path.Combine(_webRootPath, video.RawLocation);
        string fileName = Path.GetFileName(localVideoPath);
        string key = $"videos/{video.Id}/{fileName}";

        await storageService.UploadLargeFileAsync(localVideoPath, key, "application/octet-stream"); //default

        video.BackupStatus = VideoBackupStatus.BackedUp;
        await UpdateVideo(video);

        return video.BackupStatus;
    }

    public async Task UpdateResolution(Video video)
    {
        var rawLocation = Path.Combine(_webRootPath, video.RawLocation);

        var mediaInfo = await FFProbe.AnalyseAsync(rawLocation);
        var videoHeight = mediaInfo.PrimaryVideoStream!.Height;

        video.Height = videoHeight;
    }

    public async Task UploadSegmentsToStorageAsync(VideoStream videoStream)
    {
        string relativePath = Uri.UnescapeDataString(videoStream.M3U8Location);

        var fullFilePath = Path.Combine(_webRootPath, relativePath);
        var folderPath = Path.GetDirectoryName(fullFilePath)!;

        if (!Directory.Exists(folderPath))
        {
            return;
        }

        var tsFiles = Directory.GetFiles(folderPath, "*.ts");

        await Parallel.ForEachAsync(tsFiles, async (filePath, ct) =>
        {
            string fileName = Path.GetFileName(filePath);
            string key = $"videos/{videoStream.Video.Id}/{(int)videoStream.Resolution}/{fileName}";

            await storageService.UploadFileAsync(filePath, key, "video/MP2T");
        });
    }

}