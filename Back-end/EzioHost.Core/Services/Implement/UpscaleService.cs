using System.Collections.Concurrent;
using EzioHost.Core.Private;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;

namespace EzioHost.Core.Services.Implement
{
    public class UpscaleService(IDirectoryProvider directoryProvider) : IUpscaleService
    {
        private static readonly ImageEncodingParam ImageEncodingParam;
        private static readonly ImageOpenCvUpscaler Upscaler;
        private static readonly SessionOptions Options;
        private string WebRootPath => directoryProvider.GetWebRootPath();
        private string TempPath => directoryProvider.GetTempPath();
        static UpscaleService()
        {
            ImageEncodingParam = new ImageEncodingParam(ImwriteFlags.JpegQuality, 90);
            Options = new();
            Options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_DISABLE_ALL;
            //Options.AppendExecutionProvider_CUDA();//Enable GPU

            Upscaler = new ImageOpenCvUpscaler();
        }
        private static ConcurrentDictionary<Guid, InferenceSession> CacheInferenceSessions { get; } = [];

        private InferenceSession AddInferenceSession(OnnxModel onnxModel)
        {
            if (CacheInferenceSessions.TryGetValue(onnxModel.Id, out var session))
            {
                return session;
            }

            var onnxModelPath = Path.Combine(WebRootPath, onnxModel.FileLocation);
            var newInferenceSession = new InferenceSession(onnxModelPath, Options);
            if (CacheInferenceSessions.TryAdd(onnxModel.Id, newInferenceSession))
            {
                return newInferenceSession;
            }

            throw new FileLoadException("Cannot add onnx model");
        }

        public async Task UpscaleImage(OnnxModel model, string inputPath, string outputPath)
        {
            var inferenceSession = AddInferenceSession((model));

            var upscale = await Upscaler.UpscaleImageAsync(inputPath, inferenceSession, model.Scale);

            upscale.SaveImage(outputPath, ImageEncodingParam);
        }

        public Task UpscaleVideo(OnnxModel model, Video video)
        {
            throw new NotImplementedException();
        }
    }
}
