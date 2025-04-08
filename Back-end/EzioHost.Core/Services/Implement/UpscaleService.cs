using System.Collections.Concurrent;
using EzioHost.Core.Private;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.ML.OnnxRuntime;
using OnnxStack.Core.Config;
using OnnxStack.Core.Video;
using OnnxStack.FeatureExtractor.Pipelines;
using OnnxStack.ImageUpscaler.Common;
using OpenCvSharp;
using static EzioHost.Shared.Enums.VideoEnum;

namespace EzioHost.Core.Services.Implement
{
    public class UpscaleService(IDirectoryProvider directoryProvider) : IUpscaleService
    {
        private const int ConcurrentUpscaleTask = 10;
        private const string FramePattern = "frame_%05d.jpg";
        private const string FrameSearchPattern = "frame_*.jpg";

        private static readonly ImageEncodingParam ImageEncodingParam;
        private static readonly ImageOpenCvUpscaler Upscaler;
        private static readonly SessionOptions SessionOptions;
        private string TempPath => directoryProvider.GetTempPath();
        private string WebRootPath => directoryProvider.GetWebRootPath();

        static UpscaleService()
        {
            ImageEncodingParam = new ImageEncodingParam(ImwriteFlags.JpegQuality, 90);

            SessionOptions = SessionOptions.MakeSessionOptionWithCudaProvider(new OrtCUDAProviderOptions());
            Upscaler = new ImageOpenCvUpscaler();
        }

        private static ConcurrentDictionary<Guid, InferenceSession> CacheInferenceSessions { get; } = [];

        private InferenceSession GetInferenceSession(OnnxModel onnxModel)
        {
            if (CacheInferenceSessions.TryGetValue(onnxModel.Id, out var session))
            {
                return session;
            }

            var onnxModelPath = Path.Combine(WebRootPath, onnxModel.FileLocation);

            var newInferenceSession = new InferenceSession(onnxModelPath, SessionOptions);
            if (CacheInferenceSessions.TryAdd(onnxModel.Id, newInferenceSession))
            {
                return newInferenceSession;
            }

            throw new FileLoadException("Cannot add onnx model");
        }

        public async Task UpscaleImage(OnnxModel model, string inputPath, string outputPath)
        {
            var inferenceSession = GetInferenceSession((model));

            var upscale = await Upscaler.UpscaleImageAsync(inputPath, inferenceSession, model.Scale);

            upscale.SaveImage(outputPath, ImageEncodingParam);
        }

        public async Task UpscaleVideo(OnnxModel model, Video video)
        {
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
                var duration = videoStream.Duration.TotalSeconds;

                // Tính toán kích thước đầu ra sau khi upscale
                var outputWidth = originalWidth * model.Scale;
                var outputHeight = originalHeight * model.Scale;

                var frameExtractionArguments = FFMpegArguments
                    .FromFileInput(inputVideoPath)
                    .OutputToFile(Path.Combine(framesDir, FramePattern), true, options => options
                            .WithFramerate(frameRate)
                            .WithCustomArgument("-qscale:v 1")
                            .WithCustomArgument("-qmin 1")
                            .WithCustomArgument("-qmax 1")
                            .WithCustomArgument("-fps_mode cfr") 
                    );

                // Trích xuất âm thanh từ video
                var audioExtractionArguments = FFMpegArguments
                    .FromFileInput(inputVideoPath)
                    .OutputToFile(Path.Combine(tempDir, "audio.aac"), true, options => options
                        .WithAudioCodec(AudioCodec.Aac)
                    );

                await Task.WhenAll(
                    frameExtractionArguments.ProcessAsynchronously(),
                    audioExtractionArguments.ProcessAsynchronously()
                );

                var frameFiles = Directory.GetFiles(framesDir, FrameSearchPattern).OrderBy(f => f).ToList();

                var semaphore = new SemaphoreSlim(ConcurrentUpscaleTask);
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
                    .FromFileInput(Path.Combine(upscaledFramesDir, FramePattern), false, options => options
                        .WithFramerate(frameRate)
                    )
                    .OutputToFile(Path.Combine(tempDir, "video_no_audio.mp4"), true, options => options
                        .WithVideoCodec("h264_nvenc")
                        .WithCustomArgument("-b:v 8000k") // Bitrate
                        .WithCustomArgument("-pix_fmt yuv420p")        // Pixel format
                        .WithVideoFilters(filterOptions => filterOptions
                            .Scale(outputWidth, outputHeight)
                        )
                    );


                await videoCreationArguments.ProcessAsynchronously();

                var finalVideoArguments = FFMpegArguments
                    .FromFileInput(Path.Combine(tempDir, "video_no_audio.mp4"))
                    .AddFileInput(Path.Combine(tempDir, "audio.aac"))
                    .OutputToFile(outputVideoPath, true, options => options
                        .CopyChannel(Channel.Video)
                        .CopyChannel(Channel.Audio)
                    );

                await finalVideoArguments.ProcessAsynchronously();

                video.Status = VideoStatus.Ready;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        public async Task Test(OnnxModel model, Video video)
        {
            var inputVideoPath = Path.Combine(WebRootPath, video.RawLocation);
            var onnxModelPath = Path.Combine(WebRootPath, model.FileLocation);

            // Read Video Info
            var videoInfo = await OnnxVideo.FromFileAsync(inputVideoPath);

            // Create Video Stream
            var videoStream = VideoHelper.ReadVideoStreamAsync(inputVideoPath, videoInfo.FrameRate);

            // Create pipeline
            var pipeline = ImageUpscalePipeline.CreatePipeline(new UpscaleModelSet()
            {
                Name = Path.GetFileNameWithoutExtension(onnxModelPath),
                UpscaleModelConfig = new UpscaleModelConfig()
                {
                    Precision = OnnxModelPrecision.F32,
                    OnnxModelPath = onnxModelPath,
                    SampleSize = 512,
                    Channels = 3,
                    ScaleFactor = 4,
                    ExecutionProvider = ExecutionProvider.Cuda,
                },
                IsEnabled = true
            });

            // Create Pipeline
            var pipelineStream = await pipeline.RunAsync(videoInfo);

            // Write Video
            await pipelineStream.SaveAsync(@"C:\Users\Talon Ezio\Downloads\Compressed\Result.mp4");

            //Unload
            await pipeline.UnloadAsync();
        }
    }
}
