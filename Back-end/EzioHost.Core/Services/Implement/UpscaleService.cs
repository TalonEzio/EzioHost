using System.Collections.Concurrent;
using AutoMapper;
using EzioHost.Core.Private;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Domain.Entities;
using EzioHost.Domain.Settings;
using EzioHost.Shared.Events;
using EzioHost.Shared.Models;
using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using static EzioHost.Shared.Enums.VideoEnum;

namespace EzioHost.Core.Services.Implement;

public class UpscaleService(
    IDirectoryProvider directoryProvider,
    ISettingProvider settingProvider,
    IUpscaleRepository upscaleRepository,
    IVideoService videoService,
    IVideoUnitOfWork videoUnitOfWork,
    IMapper mapper,
    IM3U8PlaylistService m3U8PlaylistService,
    ILogger<UpscaleService> logger) : IUpscaleService
{
    private const int CONCURRENT_UPSCALE_TASK = 1;
    private const string FRAME_PATTERN = "frame_%05d.jpg";
    private const string FRAME_SEARCH_PATTERN = "frame_*.jpg";
    private const int MAX_CACHED_SESSIONS = 5; // Limit number of cached sessions

    private static readonly ImageEncodingParam ImageEncodingParam;
    private static readonly SessionOptions SessionOptions;

    private readonly Lazy<ImageOpenCvUpscaler> _upscalerLazy = new(() =>
        new ImageOpenCvUpscaler());

    static UpscaleService()
    {
        ImageEncodingParam = new ImageEncodingParam(ImwriteFlags.JpegQuality, 90);

        try
        {
            SessionOptions = SessionOptions.MakeSessionOptionWithCudaProvider(new OrtCUDAProviderOptions());
        }
        catch
        {
            SessionOptions = new SessionOptions();
        }
    }

    private ImageOpenCvUpscaler Upscaler => _upscalerLazy.Value;

    private VideoEncodeSettings VideoEncodeSetting => settingProvider.GetVideoEncodeSettings();

    private string TempPath => directoryProvider.GetTempPath();
    private string WebRootPath => directoryProvider.GetWebRootPath();

    private IVideoRepository VideoRepository => videoUnitOfWork.VideoRepository;
    private static ConcurrentDictionary<Guid, InferenceSession> CacheInferenceSessions { get; } = [];

    public async Task UpscaleImage(OnnxModel model, string inputPath, string outputPath)
    {
        var inferenceSession = GetInferenceSession(model);

        var upscale = await Upscaler.UpscaleImageAsync(inputPath, inferenceSession, model.Scale);

        upscale.SaveImage(outputPath, ImageEncodingParam);
    }

    public async Task UpscaleVideo(VideoUpscale videoUpscale)
    {
        await videoUnitOfWork.BeginTransactionAsync();

        var model = videoUpscale.Model;
        var video = videoUpscale.Video;

        if (model == null)
            throw new InvalidOperationException(
                $"OnnxModel with Id {videoUpscale.ModelId} was not found or has been deleted.");

        if (video == null)
            throw new InvalidOperationException(
                $"Video with Id {videoUpscale.VideoId} was not found or has been deleted.");

        videoUpscale.Resolution = VideoResolution.Upscaled;

        var inferenceSession = GetInferenceSession(model);

        var inputVideoPath = Path.Combine(WebRootPath, video.RawLocation);

        var outputVideoPath = Path.GetFileNameWithoutExtension(inputVideoPath) + "_upscaled";
        outputVideoPath = Path.Combine(Path.GetDirectoryName(inputVideoPath)!,
            outputVideoPath + Path.GetExtension(inputVideoPath));
        outputVideoPath = Path.Combine(WebRootPath, outputVideoPath);

        var tempDir = Path.Combine(TempPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var framesDir = Path.Combine(tempDir, "frames");
        Directory.CreateDirectory(framesDir);

        var upscaledFramesDir = Path.Combine(tempDir, "upscaled_frames");
        Directory.CreateDirectory(upscaledFramesDir);

        try
        {
            var mediaInfo = await FFProbe.AnalyseAsync(inputVideoPath);
            var mediaInfoPrimaryVideoStream = mediaInfo.PrimaryVideoStream!;

            var originalWidth = mediaInfoPrimaryVideoStream.Width;
            var originalHeight = mediaInfoPrimaryVideoStream.Height;
            var frameRate = mediaInfoPrimaryVideoStream.FrameRate;

            var outputWidth = originalWidth * model.Scale;
            var outputHeight = originalHeight * model.Scale;

            var frameExtractionArguments = FFMpegArguments
                .FromFileInput(inputVideoPath)
                .OutputToFile(Path.Combine(framesDir, FRAME_PATTERN), true, options => options
                    .WithFramerate(frameRate)
                    .WithCustomArgument("-qscale:v 1")
                    .WithCustomArgument("-qmin 1")
                    .WithCustomArgument("-qmax 1")
                    .WithCustomArgument("-fps_mode cfr")
                );
            var randomVideoFileName = Path.GetRandomFileName() + ".mp4";
            var randomAudioFileName = Path.GetRandomFileName() + ".aac";

            var audioExtractionArguments = FFMpegArguments
                .FromFileInput(inputVideoPath)
                .OutputToFile(Path.Combine(tempDir, randomAudioFileName), true, options => options
                    .WithAudioCodec(AudioCodec.Aac)
                );

            await Task.WhenAll(
                frameExtractionArguments.ProcessAsynchronously(),
                audioExtractionArguments.ProcessAsynchronously()
            );

            var frameFiles = Directory.GetFiles(framesDir, FRAME_SEARCH_PATTERN).OrderBy(f => f).ToList();

            var semaphore = new SemaphoreSlim(CONCURRENT_UPSCALE_TASK);
            var tasks = new List<Task>();

            foreach (var frameFile in frameFiles)
            {
                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var frameName = Path.GetFileName(frameFile);
                        var upscaledFramePath = Path.Combine(upscaledFramesDir, frameName);

                        var upscaledFrame = Upscaler.UpscaleImage(frameFile, inferenceSession, model.Scale);
                        upscaledFrame.SaveImage(upscaledFramePath, ImageEncodingParam);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);


            var videoCreationArguments = FFMpegArguments
                .FromFileInput(Path.Combine(upscaledFramesDir, FRAME_PATTERN), false, options => options
                    .WithFramerate(frameRate)
                )
                .OutputToFile(Path.Combine(tempDir, randomVideoFileName), true, options => options
                    .WithVideoCodec(VideoEncodeSetting.VideoCodec)
                    .WithCustomArgument($"-b:v {VideoEncodeSetting.UpscaleBitrateKbps}k")
                    .WithCustomArgument("-pix_fmt yuv420p")
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(outputWidth, outputHeight)
                    )
                );

            await videoCreationArguments.ProcessAsynchronously();

            var finalVideoArguments = FFMpegArguments
                .FromFileInput(Path.Combine(tempDir, randomVideoFileName))
                .AddFileInput(Path.Combine(tempDir, randomAudioFileName))
                .OutputToFile(outputVideoPath, true, options => options
                    .CopyChannel(Channel.Video)
                    .CopyChannel(Channel.Audio)
                );

            await finalVideoArguments.ProcessAsynchronously();

            videoUpscale.OutputLocation = Path.GetRelativePath(WebRootPath, outputVideoPath);
            await UpdateVideoUpscale(videoUpscale);

            var newVideoHlsStream =
                await videoService.CreateHlsVariantStream(outputVideoPath, video, videoUpscale.Resolution, model.Scale);

            await videoService.AddNewVideoStream(video, newVideoHlsStream);

            var absoluteM3U8Location = Path.Combine(WebRootPath, video.M3U8Location);
            await m3U8PlaylistService.BuildFullPlaylistAsync(video, absoluteM3U8Location);

            //Update thumbnail from upscaled video
            video.Thumbnail = await videoService.GenerateThumbnail(video);
            await VideoRepository.UpdateVideoForUnitOfWork(video);
            await videoUnitOfWork.CommitTransactionAsync();

            videoUpscale.Status = VideoUpscaleStatus.Ready;
            await UpdateVideoUpscale(videoUpscale);

            OnVideoUpscaleStreamAdded?.Invoke(this, new VideoStreamAddedEventArgs
            {
                VideoId = video.Id,
                VideoStream = mapper.Map<VideoStreamDto>(newVideoHlsStream)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error upscaling video {VideoUpscaleId} for video {VideoId}. Rolling back transaction.",
                videoUpscale.Id, videoUpscale.VideoId);
            await videoUnitOfWork.RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }


    public Task<VideoUpscale> AddNewVideoUpscale(VideoUpscale newVideoUpscale)
    {
        return upscaleRepository.AddNewVideoUpscale(newVideoUpscale);
    }

    public Task<VideoUpscale?> GetVideoUpscaleById(Guid id)
    {
        return upscaleRepository.GetVideoUpscaleById(id);
    }

    public Task<VideoUpscale> UpdateVideoUpscale(VideoUpscale updateVideoUpscale)
    {
        return upscaleRepository.UpdateVideoUpscale(updateVideoUpscale);
    }

    public Task DeleteVideoUpscale(VideoUpscale deleteVideoUpscale)
    {
        return upscaleRepository.DeleteVideoUpscale(deleteVideoUpscale);
    }

    public Task<VideoUpscale?> GetVideoNeedUpscale()
    {
        return upscaleRepository.GetVideoNeedUpscale();
    }

    public event EventHandler<VideoStreamAddedEventArgs>? OnVideoUpscaleStreamAdded;

    private InferenceSession GetInferenceSession(OnnxModel onnxModel)
    {
        if (CacheInferenceSessions.TryGetValue(onnxModel.Id, out var session)) return session;

        // Check if we've reached the cache limit and remove oldest sessions if needed
        if (CacheInferenceSessions.Count >= MAX_CACHED_SESSIONS) CleanupOldSessions();

        var onnxModelPath = Path.Combine(WebRootPath, onnxModel.FileLocation);

        if (!File.Exists(onnxModelPath)) throw new FileNotFoundException($"ONNX model file not found: {onnxModelPath}");

        var newInferenceSession = new InferenceSession(onnxModelPath, SessionOptions);
        if (CacheInferenceSessions.TryAdd(onnxModel.Id, newInferenceSession))
        {
            logger.LogInformation("Cached new inference session for model ID: {ModelId}", onnxModel.Id);
            return newInferenceSession;
        }

        // If we can't add to cache (race condition), dispose the session we created
        newInferenceSession.Dispose();
        throw new FileLoadException("Cannot add onnx model to cache");
    }

    private void CleanupOldSessions()
    {
        try
        {
            // Remove oldest sessions to make room for new ones
            var sessionsToRemove = CacheInferenceSessions.Count - MAX_CACHED_SESSIONS + 1;
            var keysToRemove = CacheInferenceSessions.Keys.Take(sessionsToRemove).ToList();

            foreach (var key in keysToRemove)
                if (CacheInferenceSessions.TryRemove(key, out var oldSession))
                {
                    oldSession.Dispose();
                    logger.LogInformation("Removed old inference session for model ID: {ModelId}", key);
                }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during inference session cleanup");
        }
    }
}