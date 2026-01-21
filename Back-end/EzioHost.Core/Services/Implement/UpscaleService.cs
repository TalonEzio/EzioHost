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
        logger.LogInformation(
            "Upscaling image. ModelId: {ModelId}, Scale: {Scale}, InputPath: {InputPath}",
            model.Id,
            model.Scale,
            inputPath);

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var inferenceSession = GetInferenceSession(model);

            var upscale = await Upscaler.UpscaleImageAsync(inputPath, inferenceSession, model.Scale);

            upscale.SaveImage(outputPath, ImageEncodingParam);

            stopwatch.Stop();
            logger.LogInformation(
                "Image upscaling completed. ModelId: {ModelId}, OutputPath: {OutputPath}, Duration: {DurationMs}ms",
                model.Id,
                outputPath,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error upscaling image. ModelId: {ModelId}, InputPath: {InputPath}",
                model.Id,
                inputPath);
            throw;
        }
    }

    public async Task UpscaleVideo(VideoUpscale videoUpscale)
    {
        var overallStopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation(
            "Starting video upscaling. VideoUpscaleId: {VideoUpscaleId}, VideoId: {VideoId}, ModelId: {ModelId}",
            videoUpscale.Id,
            videoUpscale.VideoId,
            videoUpscale.ModelId);

        await videoUnitOfWork.BeginTransactionAsync();
        logger.LogDebug("Transaction started for video upscaling {VideoUpscaleId}", videoUpscale.Id);

        var model = videoUpscale.Model;
        var video = videoUpscale.Video;

        if (model == null)
        {
            logger.LogError(
                "OnnxModel not found for upscaling. VideoUpscaleId: {VideoUpscaleId}, ModelId: {ModelId}",
                videoUpscale.Id,
                videoUpscale.ModelId);
            throw new InvalidOperationException(
                $"OnnxModel with Id {videoUpscale.ModelId} was not found or has been deleted.");
        }

        if (video == null)
        {
            logger.LogError(
                "Video not found for upscaling. VideoUpscaleId: {VideoUpscaleId}, VideoId: {VideoId}",
                videoUpscale.Id,
                videoUpscale.VideoId);
            throw new InvalidOperationException(
                $"Video with Id {videoUpscale.VideoId} was not found or has been deleted.");
        }

        logger.LogInformation(
            "Video upscaling parameters. VideoUpscaleId: {VideoUpscaleId}, Model: {ModelName}, Scale: {Scale}",
            videoUpscale.Id,
            model.Name,
            model.Scale);

        videoUpscale.Resolution = VideoResolution.Upscaled;

        var inferenceSession = GetInferenceSession(model);

        var inputVideoPath = Path.Combine(WebRootPath, video.RawLocation);

        var outputVideoPath = Path.GetFileNameWithoutExtension(inputVideoPath) + "_upscaled";
        outputVideoPath = Path.Combine(Path.GetDirectoryName(inputVideoPath)!,
            outputVideoPath + Path.GetExtension(inputVideoPath));
        outputVideoPath = Path.Combine(WebRootPath, outputVideoPath);

        var tempDir = Path.Combine(TempPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        logger.LogDebug("Created temporary directory for upscaling: {TempDir}", tempDir);

        var framesDir = Path.Combine(tempDir, "frames");
        Directory.CreateDirectory(framesDir);

        var upscaledFramesDir = Path.Combine(tempDir, "upscaled_frames");
        Directory.CreateDirectory(upscaledFramesDir);

        try
        {
            logger.LogDebug("Analyzing input video. VideoUpscaleId: {VideoUpscaleId}, InputPath: {InputPath}",
                videoUpscale.Id,
                inputVideoPath);

            var mediaInfo = await FFProbe.AnalyseAsync(inputVideoPath);
            var mediaInfoPrimaryVideoStream = mediaInfo.PrimaryVideoStream!;

            var originalWidth = mediaInfoPrimaryVideoStream.Width;
            var originalHeight = mediaInfoPrimaryVideoStream.Height;
            var frameRate = mediaInfoPrimaryVideoStream.FrameRate;

            var outputWidth = originalWidth * model.Scale;
            var outputHeight = originalHeight * model.Scale;

            logger.LogInformation(
                "Video analysis completed. VideoUpscaleId: {VideoUpscaleId}, Original: {OriginalWidth}x{OriginalHeight}, Output: {OutputWidth}x{OutputHeight}, FrameRate: {FrameRate}",
                videoUpscale.Id,
                originalWidth,
                originalHeight,
                outputWidth,
                outputHeight,
                frameRate);

            logger.LogInformation(
                "Starting frame and audio extraction. VideoUpscaleId: {VideoUpscaleId}",
                videoUpscale.Id);

            var extractionStopwatch = System.Diagnostics.Stopwatch.StartNew();

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

            extractionStopwatch.Stop();
            logger.LogInformation(
                "Frame and audio extraction completed. VideoUpscaleId: {VideoUpscaleId}, Duration: {DurationMs}ms",
                videoUpscale.Id,
                extractionStopwatch.ElapsedMilliseconds);

            var frameFiles = Directory.GetFiles(framesDir, FRAME_SEARCH_PATTERN).OrderBy(f => f).ToList();

            logger.LogInformation(
                "Found {FrameCount} frames to upscale. VideoUpscaleId: {VideoUpscaleId}",
                frameFiles.Count,
                videoUpscale.Id);

            logger.LogInformation(
                "Starting frame upscaling. VideoUpscaleId: {VideoUpscaleId}, FrameCount: {FrameCount}, ConcurrentTasks: {ConcurrentTasks}",
                videoUpscale.Id,
                frameFiles.Count,
                CONCURRENT_UPSCALE_TASK);

            var upscaleStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var semaphore = new SemaphoreSlim(CONCURRENT_UPSCALE_TASK);
            var tasks = new List<Task>();
            var processedFrames = 0;
            var failedFrames = 0;

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

                        var currentProcessed = Interlocked.Increment(ref processedFrames);
                        if (currentProcessed % 100 == 0 || currentProcessed == frameFiles.Count)
                        {
                            logger.LogDebug(
                                "Frame upscaling progress. VideoUpscaleId: {VideoUpscaleId}, Processed: {Processed}/{Total} ({Percentage:F1}%)",
                                videoUpscale.Id,
                                currentProcessed,
                                frameFiles.Count,
                                (currentProcessed * 100.0 / frameFiles.Count));
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failedFrames);
                        logger.LogWarning(ex,
                            "Error upscaling frame. VideoUpscaleId: {VideoUpscaleId}, FrameFile: {FrameFile}",
                            videoUpscale.Id,
                            frameFile);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            upscaleStopwatch.Stop();
            logger.LogInformation(
                "Frame upscaling completed. VideoUpscaleId: {VideoUpscaleId}, Processed: {Processed}, Failed: {Failed}, Total: {Total}, Duration: {DurationMs}ms",
                videoUpscale.Id,
                processedFrames,
                failedFrames,
                frameFiles.Count,
                upscaleStopwatch.ElapsedMilliseconds);


            logger.LogInformation(
                "Creating upscaled video from frames. VideoUpscaleId: {VideoUpscaleId}",
                videoUpscale.Id);

            var videoCreationStopwatch = System.Diagnostics.Stopwatch.StartNew();

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

            videoCreationStopwatch.Stop();
            logger.LogInformation(
                "Video creation from frames completed. VideoUpscaleId: {VideoUpscaleId}, Duration: {DurationMs}ms",
                videoUpscale.Id,
                videoCreationStopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Merging audio with upscaled video. VideoUpscaleId: {VideoUpscaleId}",
                videoUpscale.Id);

            var mergeStopwatch = System.Diagnostics.Stopwatch.StartNew();

            var finalVideoArguments = FFMpegArguments
                .FromFileInput(Path.Combine(tempDir, randomVideoFileName))
                .AddFileInput(Path.Combine(tempDir, randomAudioFileName))
                .OutputToFile(outputVideoPath, true, options => options
                    .CopyChannel(Channel.Video)
                    .CopyChannel(Channel.Audio)
                );

            await finalVideoArguments.ProcessAsynchronously();

            mergeStopwatch.Stop();
            logger.LogInformation(
                "Audio merge completed. VideoUpscaleId: {VideoUpscaleId}, Duration: {DurationMs}ms",
                videoUpscale.Id,
                mergeStopwatch.ElapsedMilliseconds);

            videoUpscale.OutputLocation = Path.GetRelativePath(WebRootPath, outputVideoPath);
            await UpdateVideoUpscale(videoUpscale);

            logger.LogInformation(
                "Creating HLS stream for upscaled video. VideoUpscaleId: {VideoUpscaleId}, VideoId: {VideoId}",
                videoUpscale.Id,
                video.Id);

            var newVideoHlsStream =
                await videoService.CreateHlsVariantStream(outputVideoPath, video, videoUpscale.Resolution,
                    video.CreatedBy, model.Scale);

            await videoService.AddNewVideoStream(video, newVideoHlsStream);

            var absoluteM3U8Location = Path.Combine(WebRootPath, video.M3U8Location);
            await m3U8PlaylistService.BuildFullPlaylistAsync(video, absoluteM3U8Location);

            logger.LogDebug("Updating thumbnail from upscaled video. VideoUpscaleId: {VideoUpscaleId}", videoUpscale.Id);
            //Update thumbnail from upscaled video
            video.Thumbnail = await videoService.GenerateThumbnail(video);
            await VideoRepository.UpdateVideoForUnitOfWork(video);
            await videoUnitOfWork.CommitTransactionAsync();

            logger.LogDebug("Transaction committed for video upscaling {VideoUpscaleId}", videoUpscale.Id);

            videoUpscale.Status = VideoUpscaleStatus.Ready;
            await UpdateVideoUpscale(videoUpscale);

            overallStopwatch.Stop();
            logger.LogInformation(
                "Video upscaling completed successfully. VideoUpscaleId: {VideoUpscaleId}, VideoId: {VideoId}, TotalDuration: {DurationMs}ms",
                videoUpscale.Id,
                video.Id,
                overallStopwatch.ElapsedMilliseconds);

            OnVideoUpscaleStreamAdded?.Invoke(this, new VideoStreamAddedEventArgs
            {
                VideoId = video.Id,
                VideoStream = mapper.Map<VideoStreamDto>(newVideoHlsStream)
            });
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            logger.LogError(ex,
                "Error upscaling video {VideoUpscaleId} for video {VideoId} after {DurationMs}ms. Rolling back transaction.",
                videoUpscale.Id,
                videoUpscale.VideoId,
                overallStopwatch.ElapsedMilliseconds);
            await videoUnitOfWork.RollbackTransactionAsync();
            logger.LogDebug("Transaction rolled back for video upscaling {VideoUpscaleId}", videoUpscale.Id);
            throw;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                    logger.LogDebug("Cleaned up temporary directory: {TempDir}", tempDir);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error cleaning up temporary directory: {TempDir}", tempDir);
                }
            }
        }
    }


    public async Task<VideoUpscale> AddNewVideoUpscale(VideoUpscale newVideoUpscale)
    {
        logger.LogInformation(
            "Adding new video upscale. VideoUpscaleId: {VideoUpscaleId}, VideoId: {VideoId}, ModelId: {ModelId}",
            newVideoUpscale.Id,
            newVideoUpscale.VideoId,
            newVideoUpscale.ModelId);

        var result = await upscaleRepository.AddNewVideoUpscale(newVideoUpscale);
        logger.LogInformation("Successfully added video upscale {VideoUpscaleId}", newVideoUpscale.Id);
        return result;
    }

    public Task<VideoUpscale?> GetVideoUpscaleById(Guid id)
    {
        logger.LogDebug("Getting video upscale by ID: {VideoUpscaleId}", id);
        return upscaleRepository.GetVideoUpscaleById(id);
    }

    public async Task<VideoUpscale> UpdateVideoUpscale(VideoUpscale updateVideoUpscale)
    {
        logger.LogDebug(
            "Updating video upscale. VideoUpscaleId: {VideoUpscaleId}, Status: {Status}",
            updateVideoUpscale.Id,
            updateVideoUpscale.Status);

        var result = await upscaleRepository.UpdateVideoUpscale(updateVideoUpscale);
        return result;
    }

    public async Task DeleteVideoUpscale(VideoUpscale deleteVideoUpscale)
    {
        logger.LogInformation(
            "Deleting video upscale. VideoUpscaleId: {VideoUpscaleId}, VideoId: {VideoId}",
            deleteVideoUpscale.Id,
            deleteVideoUpscale.VideoId);

        await upscaleRepository.DeleteVideoUpscale(deleteVideoUpscale);
        logger.LogInformation("Successfully deleted video upscale {VideoUpscaleId}", deleteVideoUpscale.Id);
    }

    public Task<VideoUpscale?> GetVideoNeedUpscale()
    {
        logger.LogDebug("Getting video that needs upscaling");
        return upscaleRepository.GetVideoNeedUpscale();
    }

    public event EventHandler<VideoStreamAddedEventArgs>? OnVideoUpscaleStreamAdded;

    private InferenceSession GetInferenceSession(OnnxModel onnxModel)
    {
        if (CacheInferenceSessions.TryGetValue(onnxModel.Id, out var session))
        {
            logger.LogDebug("Using cached inference session for model ID: {ModelId}", onnxModel.Id);
            return session;
        }

        logger.LogDebug(
            "Inference session not found in cache. ModelId: {ModelId}, CacheSize: {CacheSize}/{MaxCacheSize}",
            onnxModel.Id,
            CacheInferenceSessions.Count,
            MAX_CACHED_SESSIONS);

        // Check if we've reached the cache limit and remove oldest sessions if needed
        if (CacheInferenceSessions.Count >= MAX_CACHED_SESSIONS)
        {
            logger.LogDebug("Cache limit reached, cleaning up old sessions");
            CleanupOldSessions();
        }

        var onnxModelPath = Path.Combine(WebRootPath, onnxModel.FileLocation);

        if (!File.Exists(onnxModelPath))
        {
            logger.LogError("ONNX model file not found. ModelId: {ModelId}, Path: {Path}", onnxModel.Id, onnxModelPath);
            throw new FileNotFoundException($"ONNX model file not found: {onnxModelPath}");
        }

        logger.LogInformation(
            "Loading inference session from file. ModelId: {ModelId}, Path: {Path}",
            onnxModel.Id,
            onnxModelPath);

        var newInferenceSession = new InferenceSession(onnxModelPath, SessionOptions);
        if (CacheInferenceSessions.TryAdd(onnxModel.Id, newInferenceSession))
        {
            logger.LogInformation(
                "Cached new inference session for model ID: {ModelId}. CacheSize: {CacheSize}",
                onnxModel.Id,
                CacheInferenceSessions.Count);
            return newInferenceSession;
        }

        // If we can't add to cache (race condition), dispose the session we created
        logger.LogWarning(
            "Failed to add inference session to cache (race condition). ModelId: {ModelId}",
            onnxModel.Id);
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

            logger.LogDebug(
                "Cleaning up {Count} old inference sessions. CurrentCacheSize: {CacheSize}",
                sessionsToRemove,
                CacheInferenceSessions.Count);

            var removedCount = 0;
            foreach (var key in keysToRemove)
                if (CacheInferenceSessions.TryRemove(key, out var oldSession))
                {
                    oldSession.Dispose();
                    removedCount++;
                    logger.LogDebug("Removed old inference session for model ID: {ModelId}", key);
                }

            logger.LogInformation(
                "Inference session cleanup completed. Removed: {RemovedCount}, RemainingCacheSize: {CacheSize}",
                removedCount,
                CacheInferenceSessions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during inference session cleanup");
        }
    }
}