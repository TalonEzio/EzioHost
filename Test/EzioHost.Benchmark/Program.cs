using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Runtime.InteropServices;

namespace EzioHost.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TensorToImageBenchmark>();
            Console.WriteLine(summary);

            //new TensorToImageBenchmark().Setup();
        }
    }

    [MemoryDiagnoser]
    public class TensorToImageBenchmark
    {
        private Tensor<float> _tensor = null!;
        private int _width;
        private int _height;

        [GlobalSetup]
        public void Setup()
        {
            // Thiết lập test data
            var options = new SessionOptions();
            options.AppendExecutionProvider_CUDA();
            var session = new InferenceSession("model.onnx", options);

            const string imagePath = "input.jpg";
            const int scale = 4;

            var inputName = session.InputMetadata.Keys.First();
            var inputImageBgr = Cv2.ImRead(imagePath);
            var inputTensor = ImageToTensor(inputImageBgr);

            var results = session.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) });
            _tensor = results.First().AsTensor<float>();
            _width = inputImageBgr.Width * scale;
            _height = inputImageBgr.Height * scale;
        }

        private static Tensor<float> ImageToTensor(Mat image)
        {
            int width = image.Width;
            int height = image.Height;

            // OpenCV dùng BGR, đổi thứ tự nếu cần RGB
            var tensorData = new float[1 * 3 * height * width];
            var data = new byte[height * width * 3];
            Marshal.Copy(image.Data, data, 0, data.Length);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = (y * width + x) * 3;
                    // BGR -> RGB
                    byte b = data[idx];
                    byte g = data[idx + 1];
                    byte r = data[idx + 2];

                    int baseIdx = y * width + x;
                    tensorData[0 * height * width + baseIdx] = r / 255.0f;
                    tensorData[1 * height * width + baseIdx] = g / 255.0f;
                    tensorData[2 * height * width + baseIdx] = b / 255.0f;
                }
            }

            return new DenseTensor<float>(tensorData, new[] { 1, 3, height, width });
        }


        [Benchmark]
        public unsafe Mat TensorToImage_Original()
        {
            var output = new Mat(new Size(_width, _height), MatType.CV_8UC3);
            var data = output.DataPointer;

            Parallel.For(0, _height, y =>
            {
                for (var x = 0; x < _width; x++)
                {
                    var r = Math.Clamp((int)(_tensor[0, 0, y, x] * 255), 0, 255);
                    var g = Math.Clamp((int)(_tensor[0, 1, y, x] * 255), 0, 255);
                    var b = Math.Clamp((int)(_tensor[0, 2, y, x] * 255), 0, 255);
                    var index = (y * _width + x) * 3;
                    data[index] = (byte)b;
                    data[index + 1] = (byte)g;
                    data[index + 2] = (byte)r;
                }
            });

            return output;
        }

        [Benchmark]
        public unsafe Mat TensorToImage_WithSpan()
        {
            var output = new Mat(new Size(_width, _height), MatType.CV_8UC3);
            var outputSpan = new Span<byte>(output.DataPointer, _width * _height * 3);

            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    int index = (y * _width + x) * 3;
                    byte r = (byte)Math.Clamp((int)(_tensor[0, 0, y, x] * 255), 0, 255);
                    byte g = (byte)Math.Clamp((int)(_tensor[0, 1, y, x] * 255), 0, 255);
                    byte b = (byte)Math.Clamp((int)(_tensor[0, 2, y, x] * 255), 0, 255);
                    outputSpan[index] = b;
                    outputSpan[index + 1] = g;
                    outputSpan[index + 2] = r;
                }
            }

            return output;
        }

        [Benchmark]
        public unsafe Mat TensorToImage_CombinedApproach()
        {
            var output = new Mat(new Size(_width, _height), MatType.CV_8UC3);
            byte* data = output.DataPointer;

            Parallel.For(0, _height, y =>
            {
                for (var x = 0; x < _width; x++)
                {
                    byte r = (byte)Math.Clamp((int)(_tensor[0, 0, y, x] * 255), 0, 255);
                    byte g = (byte)Math.Clamp((int)(_tensor[0, 1, y, x] * 255), 0, 255);
                    byte b = (byte)Math.Clamp((int)(_tensor[0, 2, y, x] * 255), 0, 255);

                    int index = (y * _width + x) * 3;
                    data[index] = b;
                    data[index + 1] = g;
                    data[index + 2] = r;
                }
            });

            return output;
        }

        [Benchmark]
        public unsafe Mat TensorToImage_ChunkApproach()
        {
            var output = new Mat(new Size(_width, _height), MatType.CV_8UC3);
            byte* data = (byte*)output.DataPointer;

            int chunkSize = _height / Environment.ProcessorCount;
            if (chunkSize == 0) chunkSize = 1; // Đảm bảo chunkSize ít nhất là 1

            Parallel.For(0, Environment.ProcessorCount, i =>
            {
                int startY = i * chunkSize;
                int endY = (i == Environment.ProcessorCount - 1) ? _height : (i + 1) * chunkSize;

                // Mỗi luồng làm việc với một phần riêng biệt của ảnh
                for (int y = startY; y < endY; y++)
                {
                    for (var x = 0; x < _width; x++)
                    {
                        byte r = (byte)Math.Clamp((int)(_tensor[0, 0, y, x] * 255), 0, 255);
                        byte g = (byte)Math.Clamp((int)(_tensor[0, 1, y, x] * 255), 0, 255);
                        byte b = (byte)Math.Clamp((int)(_tensor[0, 2, y, x] * 255), 0, 255);
                        int index = (y * _width + x) * 3;
                        data[index] = b;
                        data[index + 1] = g;
                        data[index + 2] = r;
                    }
                }
            });

            return output;
        }

    }
}