using System.Collections.Concurrent;
using System.Text;
using EzioHost.Core.Private;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Extensions;
using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using static EzioHost.Shared.Enums.VideoEnum;

namespace EzioHost.Core.Services.Implement;

public class UpscaleService(
    IDirectoryProvider directoryProvider,
    IUpscaleRepository upscaleRepository,
    IVideoService videoService,
    IVideoUnitOfWork videoUnitOfWork) : IUpscaleService
{
    private const int CONCURRENT_UPSCALE_TASK = 1;
    private const string FRAME_PATTERN = "frame_%05d.jpg";
    private const string FRAME_SEARCH_PATTERN = "frame_*.jpg";

    private static readonly ImageEncodingParam ImageEncodingParam;
    private static readonly ImageOpenCvUpscaler Upscaler;
    private static readonly SessionOptions SessionOptions;


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

        Upscaler = new ImageOpenCvUpscaler();
    }

    private string TempPath => directoryProvider.GetTempPath();
    private string WebRootPath => directoryProvider.GetWebRootPath();

    private IVideoRepository VideoRepository => videoUnitOfWork.VideoRepository;
    private IVideoStreamRepository VideoStreamRepository => videoUnitOfWork.VideoStreamRepository;

    private static ConcurrentDictionary<Guid, InferenceSession> CacheInferenceSessions { get; } = [];

    public async Task UpscaleImage(OnnxModel model, string inputPath, string outputPath)
    {
        var inferenceSession = GetInferenceSession(model);

        var upscale = await Upscaler.UpscaleImageAsync(inputPath, inferenceSession, model.Scale);

        upscale.SaveImage(outputPath, ImageEncodingParam);
    }

    public async Task UpscaleVideo(VideoUpscale videoUpscale)
    {
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

        // Đường dẫn đến file video gốc
        var inputVideoPath = Path.Combine(WebRootPath, video.RawLocation);

        // Đường dẫn đến file video đầu ra
        var outputVideoPath = Path.GetFileNameWithoutExtension(inputVideoPath) + "_upscaled";
        outputVideoPath = Path.Combine(Path.GetDirectoryName(inputVideoPath)!,
            outputVideoPath + Path.GetExtension(inputVideoPath));
        outputVideoPath = Path.Combine(WebRootPath, outputVideoPath);

        // Tạo thư mục tạm để lưu các frame
        var tempDir = Path.Combine(TempPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Tạo thư mục cho các frame đã upscale
        var framesDir = Path.Combine(tempDir, "frames");
        Directory.CreateDirectory(framesDir);

        // Tạo thư mục cho các frame đã upscale
        var upscaledFramesDir = Path.Combine(tempDir, "upscaled_frames");
        Directory.CreateDirectory(upscaledFramesDir);

        try
        {
            // Đọc thông tin video
            var mediaInfo = await FFProbe.AnalyseAsync(inputVideoPath);
            var videoStream = mediaInfo.PrimaryVideoStream!;

            // Lấy thông tin video
            var originalWidth = videoStream.Width;
            var originalHeight = videoStream.Height;
            var frameRate = videoStream.FrameRate;

            // Tính toán kích thước đầu ra sau khi upscale
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

            // Trích xuất âm thanh từ video
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
                    .WithVideoCodec("h264_nvenc")
                    .WithCustomArgument("-b:v 8000k")
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

            await videoUnitOfWork.BeginTransactionAsync();

            VideoStreamRepository.Create(newVideoHlsStream);
            video.VideoStreams.Add(newVideoHlsStream);

            var currentResolution = videoUpscale.Resolution.GetDescription();
            var filePath = Path.Combine(currentResolution, Path.GetFileName(newVideoHlsStream.M3U8Location))
                .Replace("\\", "/");

            var m3U8ContentBuilder = new StringBuilder();
            m3U8ContentBuilder.AppendLine(
                $"#EXT-X-STREAM-INF:BANDWIDTH={videoService.GetBandwidthForResolution(currentResolution)},RESOLUTION={videoService.GetResolutionDimensions(currentResolution)}");
            m3U8ContentBuilder.AppendLine(filePath);

            await using var m3U8MergeFileStream = new FileStream(
                Path.Combine(WebRootPath, video.M3U8Location),
                FileMode.OpenOrCreate,
                FileAccess.Write);
            m3U8MergeFileStream.Seek(0, SeekOrigin.End);
            await m3U8MergeFileStream.WriteAsync(Encoding.UTF8.GetBytes(m3U8ContentBuilder.ToString()));


            await VideoRepository.UpdateVideoForUnitOfWork(video);
            await videoUnitOfWork.CommitTransactionAsync();

            videoUpscale.Status = VideoUpscaleStatus.Ready;
            await UpdateVideoUpscale(videoUpscale);
        }
        catch
        {
            await videoUnitOfWork.RollbackTransactionAsync();
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

    private InferenceSession GetInferenceSession(OnnxModel onnxModel)
    {
        if (CacheInferenceSessions.TryGetValue(onnxModel.Id, out var session)) return session;

        var onnxModelPath = Path.Combine(WebRootPath, onnxModel.FileLocation);

        var newInferenceSession = new InferenceSession(onnxModelPath, SessionOptions);
        if (CacheInferenceSessions.TryAdd(onnxModel.Id, newInferenceSession)) return newInferenceSession;

        throw new FileLoadException("Cannot add onnx model");
    }
}