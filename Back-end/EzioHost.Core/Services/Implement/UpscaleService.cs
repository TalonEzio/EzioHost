using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
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
using Channel = System.Threading.Channels.Channel;

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
    private const int CONCURRENT_UPSCALE_TASK = 2; // Increased for streaming pipeline
    private const int BATCH_SIZE = 1; // Batch size for upscaling (can be increased if model supports it)
    private const int MAX_CACHED_SESSIONS = 5; // Limit number of cached sessions

    // Records for streaming pipeline
    private record struct FrameData(int Index, byte[] Data, int Width, int Height);
    private record struct UpscaledFrameData(int Index, byte[] Data, int Width, int Height);

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
            var stopwatch = Stopwatch.StartNew();
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

    /// <summary>
    /// Streaming pipeline: decode frames from FFmpeg stdout, upscale, and encode back to FFmpeg stdin
    /// This avoids saving all frames to disk, significantly reducing I/O and disk space usage
    /// </summary>
    private async Task ProcessVideoStreamingAsync(
        string inputVideoPath,
        string outputVideoPath,
        string audioPath,
        int originalWidth,
        int originalHeight,
        double frameRate,
        int outputWidth,
        int outputHeight,
        InferenceSession inferenceSession,
        int scale,
        Guid videoUpscaleId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Starting streaming pipeline. VideoUpscaleId: {VideoUpscaleId}, Workers: {Workers}, BatchSize: {BatchSize}",
            videoUpscaleId,
            CONCURRENT_UPSCALE_TASK,
            BATCH_SIZE);

        var upscaleStopwatch = Stopwatch.StartNew();
        var processedFrames = new ThreadSafeCounter();
        var failedFrames = new ThreadSafeCounter();
        var sessionLock = new object();

        // Start FFmpeg decode process (outputs raw BGR frames to stdout)
        using var decodeProcess = StartDecodeProcess(inputVideoPath, originalWidth, originalHeight, frameRate);
        if (decodeProcess.StandardOutput.BaseStream == Stream.Null)
        {
            throw new InvalidOperationException("Failed to access FFmpeg decode pipe.");
        }

        // Drain decode stderr to avoid blocking
        var decodeErrorTask = DrainStreamAsync(decodeProcess.StandardError, "ffmpeg-decode", cancellationToken);

        // Create channels for pipeline stages
        var inputChannel = Channel.CreateUnbounded<FrameData>();
        var outputChannel = Channel.CreateUnbounded<UpscaledFrameData>();

        var source = decodeProcess.StandardOutput.BaseStream;
        var frameSize = originalWidth * originalHeight * 3; // BGR24 = 3 bytes per pixel
        const int readBatchSize = 4; // Read multiple frames at once to reduce syscalls
        var batchBuffer = new byte[frameSize * readBatchSize];
        var frameIndex = 0;

        // Stage 1: Read frames from FFmpeg stdout
        var readTask = Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var totalBytesRead = 0;
                    var targetBytes = frameSize * readBatchSize;

                    while (totalBytesRead < targetBytes)
                    {
                        var read = await source.ReadAsync(
                            batchBuffer.AsMemory(totalBytesRead, targetBytes - totalBytesRead),
                            cancellationToken);
                        if (read == 0)
                        {
                            // End of stream: emit remaining full frames
                            if (totalBytesRead > 0 && totalBytesRead % frameSize == 0)
                            {
                                var framesInBatch = totalBytesRead / frameSize;
                                for (int i = 0; i < framesInBatch; i++)
                                {
                                    var offset = i * frameSize;
                                    var frameData = new byte[frameSize];
                                    Array.Copy(batchBuffer, offset, frameData, 0, frameSize);
                                    await inputChannel.Writer.WriteAsync(
                                        new FrameData(frameIndex, frameData, originalWidth, originalHeight),
                                        cancellationToken);
                                    frameIndex++;
                                }
                            }
                            return;
                        }
                        totalBytesRead += read;
                    }

                    // Split batch into individual frames
                    for (int i = 0; i < readBatchSize; i++)
                    {
                        var offset = i * frameSize;
                        var frameData = new byte[frameSize];
                        Array.Copy(batchBuffer, offset, frameData, 0, frameSize);
                        await inputChannel.Writer.WriteAsync(
                            new FrameData(frameIndex, frameData, originalWidth, originalHeight),
                            cancellationToken);
                        frameIndex++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Frame reading cancelled. VideoUpscaleId: {VideoUpscaleId}", videoUpscaleId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading frames. VideoUpscaleId: {VideoUpscaleId}", videoUpscaleId);
            }
            finally
            {
                inputChannel.Writer.Complete();
            }
        }, cancellationToken);

        // Stage 2: Worker tasks to upscale frames
        var workerTasks = new List<Task>();
        for (int i = 0; i < CONCURRENT_UPSCALE_TASK; i++)
        {
            workerTasks.Add(Task.Run(async () =>
            {
                var batch = new List<FrameData>(BATCH_SIZE);
                await foreach (var frameData in inputChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    batch.Add(frameData);

                    if (batch.Count >= BATCH_SIZE)
                    {
                        await ProcessBatchAsync(
                            batch,
                            outputChannel,
                            inferenceSession,
                            scale,
                            sessionLock,
                            videoUpscaleId,
                            processedFrames,
                            failedFrames,
                            cancellationToken);
                        batch.Clear();
                    }
                }

                // Process remaining frames
                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(
                        batch,
                        outputChannel,
                        inferenceSession,
                        scale,
                        sessionLock,
                        videoUpscaleId,
                        processedFrames,
                        failedFrames,
                        cancellationToken);
                }
            }, cancellationToken));
        }

        // Stage 3: Write upscaled frames to FFmpeg encode process (in order)
        Process? encodeProcess = null;
        Stream? encodeStream = null;
        Task? encodeErrorTask = null;
        var nextExpectedIndex = 0;
        var pendingFrames = new Dictionary<int, UpscaledFrameData>();
        byte[]? outputBuffer = null;

        var writeTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var upscaledData in outputChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    pendingFrames[upscaledData.Index] = upscaledData;

                    // Write frames in order
                    while (pendingFrames.TryGetValue(nextExpectedIndex, out var frameToWrite))
                    {
                        pendingFrames.Remove(nextExpectedIndex);

                        // Lazy start encoder when we have first frame
                        if (encodeProcess == null)
                        {
                            encodeProcess = StartEncodeProcess(
                                audioPath,
                                outputVideoPath,
                                frameToWrite.Width,
                                frameToWrite.Height,
                                frameRate);
                            encodeStream = encodeProcess.StandardInput.BaseStream;
                            encodeErrorTask = DrainStreamAsync(encodeProcess.StandardError, "ffmpeg-encode", cancellationToken);
                        }

                        outputBuffer ??= new byte[frameToWrite.Width * frameToWrite.Height * 3];
                        frameToWrite.Data.CopyTo(outputBuffer);

                        await encodeStream!.WriteAsync(outputBuffer, cancellationToken);
                        nextExpectedIndex++;

                        if (nextExpectedIndex % 100 == 0)
                        {
                            logger.LogDebug(
                                "Streaming progress. VideoUpscaleId: {VideoUpscaleId}, Processed: {Processed}",
                                videoUpscaleId,
                                nextExpectedIndex);
                        }
                    }
                }

                // Write remaining buffered frames
                while (pendingFrames.Count > 0 && !cancellationToken.IsCancellationRequested)
                {
                    if (pendingFrames.Remove(nextExpectedIndex, out var frameToWrite))
                    {
                        if (encodeProcess == null)
                        {
                            encodeProcess = StartEncodeProcess(
                                audioPath,
                                outputVideoPath,
                                frameToWrite.Width,
                                frameToWrite.Height,
                                frameRate);
                            encodeStream = encodeProcess.StandardInput.BaseStream;
                            encodeErrorTask = DrainStreamAsync(encodeProcess.StandardError, "ffmpeg-encode", cancellationToken);
                        }

                        outputBuffer ??= new byte[frameToWrite.Width * frameToWrite.Height * 3];
                        frameToWrite.Data.CopyTo(outputBuffer);
                        await encodeStream!.WriteAsync(outputBuffer, cancellationToken);
                        nextExpectedIndex++;
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Frame writing cancelled. VideoUpscaleId: {VideoUpscaleId}", videoUpscaleId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error writing frames. VideoUpscaleId: {VideoUpscaleId}", videoUpscaleId);
            }
        }, cancellationToken);

        // Wait for all stages
        await readTask;
        await Task.WhenAll(workerTasks);
        outputChannel.Writer.Complete();
        await writeTask;

        // Finalize encoder
        if (encodeStream != null)
        {
            await encodeStream.FlushAsync(cancellationToken);
            await encodeStream.DisposeAsync();
        }

        await decodeErrorTask;
        if (!decodeProcess.HasExited)
        {
            await decodeProcess.WaitForExitAsync(cancellationToken);
        }

        if (encodeProcess != null)
        {
            await (encodeErrorTask ?? Task.CompletedTask);
            if (!encodeProcess.HasExited)
            {
                await encodeProcess.WaitForExitAsync(cancellationToken);
            }
        }

        upscaleStopwatch.Stop();
        logger.LogInformation(
            "Streaming pipeline completed. VideoUpscaleId: {VideoUpscaleId}, Processed: {Processed}, Failed: {Failed}, Duration: {DurationMs}ms",
            videoUpscaleId,
            processedFrames.Value,
            failedFrames.Value,
            upscaleStopwatch.ElapsedMilliseconds);
    }

    private async Task ProcessBatchAsync(
        List<FrameData> batch,
        ChannelWriter<UpscaledFrameData> outputWriter,
        InferenceSession inferenceSession,
        int scale,
        object sessionLock,
        Guid videoUpscaleId,
        ThreadSafeCounter processedFrames,
        ThreadSafeCounter failedFrames,
        CancellationToken cancellationToken)
    {
        try
        {
            var inputFrames = batch.Select(f => (BgrData: f.Data, f.Width, f.Height)).ToList();
            var indices = batch.Select(f => f.Index).ToList();

            List<byte[]> upscaledFrames;
            int outputWidth, outputHeight;

            lock (sessionLock)
            {
                (upscaledFrames, outputWidth, outputHeight) = Upscaler.UpscaleBatchFromBgrBytes(
                    inputFrames,
                    inferenceSession,
                    scale);
            }

            for (int i = 0; i < upscaledFrames.Count; i++)
            {
                await outputWriter.WriteAsync(
                    new UpscaledFrameData(indices[i], upscaledFrames[i], outputWidth, outputHeight),
                    cancellationToken);
            }

            processedFrames.Add(batch.Count);
        }
        catch (Exception ex)
        {
            failedFrames.Add(batch.Count);
            logger.LogWarning(ex,
                "Error upscaling batch. VideoUpscaleId: {VideoUpscaleId}, BatchSize: {BatchSize}",
                videoUpscaleId,
                batch.Count);
        }
    }

    private Process StartDecodeProcess(string inputVideo, int width, int height, double frameRate)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-v error -i \"{inputVideo}\" -f rawvideo -pix_fmt bgr24 -s {width}x{height} -r {frameRate.ToString(CultureInfo.InvariantCulture)} pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return Process.Start(psi) ?? throw new InvalidOperationException("Unable to start FFmpeg decode process.");
    }

    private Process StartEncodeProcess(string audioSource, string outputVideo, int width, int height, double frameRate)
    {
        var videoCodec = VideoEncodeSetting.VideoCodec.ToLowerInvariant();
        var bitrate = VideoEncodeSetting.UpscaleBitrateKbps;

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments =
                $"-y -f rawvideo -pix_fmt bgr24 -s {width}x{height} -r {frameRate.ToString(CultureInfo.InvariantCulture)} -i pipe:0 " +
                $"-i \"{audioSource}\" -map 0:v:0 -map 1:a? -c:v {videoCodec} -b:v {bitrate}k -pix_fmt yuv420p -c:a copy -shortest \"{outputVideo}\"",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return Process.Start(psi) ?? throw new InvalidOperationException("Unable to start FFmpeg encode process.");
    }

    private async Task DrainStreamAsync(StreamReader reader, string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;
                // Optionally log FFmpeg output for debugging
                // logger.LogTrace("{Prefix}: {Line}", prefix, line);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
    }

    public async Task UpscaleVideo(VideoUpscale videoUpscale)
    {
        var overallStopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Starting video upscaling. VideoUpscaleId: {VideoUpscaleId}, VideoId: {VideoId}, ModelId: {ModelId}",
            videoUpscale.Id,
            videoUpscale.VideoId,
            videoUpscale.ModelId);

        await videoUnitOfWork.BeginTransactionAsync();
        logger.LogDebug("Transaction started for video upscaling {VideoUpscaleId}", videoUpscale.Id);

        var model = videoUpscale.Model;
        var video = videoUpscale.Video;

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
                "Starting streaming pipeline upscaling. VideoUpscaleId: {VideoUpscaleId}",
                videoUpscale.Id);

            // Extract audio first (still needed for merging)
            var randomAudioFileName = Path.GetRandomFileName() + ".aac";
            var audioPath = Path.Combine(tempDir, randomAudioFileName);

            logger.LogInformation(
                "Extracting audio. VideoUpscaleId: {VideoUpscaleId}",
                videoUpscale.Id);

            var audioExtractionArguments = FFMpegArguments
                .FromFileInput(inputVideoPath)
                .OutputToFile(audioPath, true, options => options
                    .WithAudioCodec(AudioCodec.Aac)
                );

            await audioExtractionArguments.ProcessAsynchronously();

            logger.LogInformation(
                "Audio extraction completed. VideoUpscaleId: {VideoUpscaleId}",
                videoUpscale.Id);

            // Use streaming pipeline: decode -> upscale -> encode (all in memory, no disk I/O for frames)
            var tempVideoPath = Path.Combine(tempDir, Path.GetRandomFileName() + ".mp4");
            await ProcessVideoStreamingAsync(
                inputVideoPath,
                tempVideoPath,
                audioPath,
                originalWidth,
                originalHeight,
                frameRate,
                outputWidth,
                outputHeight,
                inferenceSession,
                model.Scale,
                videoUpscale.Id);

            // Move temp video to final location
            if (File.Exists(tempVideoPath))
            {
                File.Move(tempVideoPath, outputVideoPath, true);
                logger.LogInformation(
                    "Streaming pipeline completed and video saved. VideoUpscaleId: {VideoUpscaleId}",
                    videoUpscale.Id);
            }
            else
            {
                throw new InvalidOperationException($"Streaming pipeline failed: output video not found at {tempVideoPath}");
            }

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

    private class ThreadSafeCounter
    {
        private int _value;

        public int Value => Interlocked.CompareExchange(ref _value, 0, 0);

        public void Add(int amount)
        {
            Interlocked.Add(ref _value, amount);
        }
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