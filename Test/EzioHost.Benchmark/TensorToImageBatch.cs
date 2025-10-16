using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Buffers;
using System.Diagnostics;

namespace EzioHost.Benchmark
{
    public class TensorToImageBatch
    {
        private static InferenceSession _session = null!;

        private readonly int _batchSize;
        private readonly int _width;
        private readonly int _height;

        private static readonly ImageEncodingParam SaveImageEncodingParam = new ImageEncodingParam(ImwriteFlags.JpegQuality, 90);

        private int Hw => _width * _height;

        private readonly ArrayPool<float> _arrayPool = ArrayPool<float>.Shared;
        public TensorToImageBatch(int batchSize, int width, int height)
        {
            var options = new SessionOptions();
            options.AppendExecutionProvider_CUDA();
            _session = new InferenceSession("model.onnx", options);

            _batchSize = batchSize;
            _width = width;
            _height = height;
        }

        public List<DenseTensor<float>> ImageToTensor(string folderImagePath)
        {
            var imageRead = Cv2.ImReadMulti(folderImagePath, out var mats);
            if (!imageRead && (mats is null || mats.Length == 0)) throw new FileNotFoundException("Not found image");
            return ImageToTensor(mats.ToList());
        }
        public List<DenseTensor<float>> ImageToTensor(List<string> imagePaths)
        {
            return ImageToTensor(imagePaths.Select(x => Cv2.ImRead(x)).ToList());
        }

        public unsafe List<DenseTensor<float>> ImageToTensor(List<Mat> images)
        {
            var stopwatch = Stopwatch.StartNew();

            var tensorList = new List<DenseTensor<float>>();

            int batchCount = (int)Math.Ceiling(images.Count / (float)_batchSize);

            // Ghi log thời gian để đo số lượng batch cần xử lý
            Console.WriteLine($"Total batches to process: {batchCount}");

            for (int b = 0; b < batchCount; b++)
            {
                var batchStopwatch = Stopwatch.StartNew(); // Đo thời gian cho từng batch

                int offset = b * _batchSize;
                int actualBatch = Math.Min(_batchSize, images.Count - offset);
                int totalElements = actualBatch * 3 * _height * _width;

                var batchData = _arrayPool.Rent(totalElements);
                Array.Clear(batchData, 0, totalElements);

                for (int i = 0; i < actualBatch; i++)
                {
                    var img = images[offset + i];
                    var ptr = img.DataPointer;

                    for (int j = 0; j < Hw; j++)
                    {
                        int pixelIdx = j * 3;

                        var bVal = ptr[pixelIdx + 0];
                        var gVal = ptr[pixelIdx + 1];
                        var rVal = ptr[pixelIdx + 2];

                        batchData[i * 3 * Hw + 0 * Hw + j] = rVal / 255.0f; // R
                        batchData[i * 3 * Hw + 1 * Hw + j] = gVal / 255.0f; // G
                        batchData[i * 3 * Hw + 2 * Hw + j] = bVal / 255.0f; // B
                    }
                }

                int[] dims = { actualBatch, 3, _height, _width };
                var tensor = new DenseTensor<float>(new Memory<float>(batchData, 0, totalElements), dims);
                tensorList.Add(tensor);

                // Ghi log thời gian hoàn thành một batch
                batchStopwatch.Stop();
                Console.WriteLine($"Batch {b + 1} processed in {batchStopwatch.ElapsedMilliseconds} ms");
            }


            stopwatch.Stop(); // Dừng stopwatch sau khi tất cả đã xử lý
            Console.WriteLine($"Total processing time: {stopwatch.ElapsedMilliseconds} ms");

            return tensorList;
        }


        public Tensor<float> UpscaleImage(DenseTensor<float> inputTensor)
        {
            var inputName = _session.InputMetadata.Keys.First();
            using var results = _session.Run([NamedOnnxValue.CreateFromTensor(inputName, inputTensor)]);
            return results.First().AsTensor<float>();
        }
        public unsafe List<Mat> TensorToImageWithUpscale(List<DenseTensor<float>> inputBatches)
        {
            var results = new List<Mat>();
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Start processing batches...");

            foreach (var input in inputBatches)
            {
                var batchStopwatch = Stopwatch.StartNew(); // Đo thời gian cho mỗi batch
                Console.WriteLine("Processing a new batch...");

                var upscaled = UpscaleImage(input); // ONNX output: DenseTensor<float>
                var span = ((DenseTensor<float>)upscaled).Buffer.Span;

                int batch = upscaled.Dimensions[0];
                int channels = upscaled.Dimensions[1];
                int height = upscaled.Dimensions[2];
                int width = upscaled.Dimensions[3];

                int hw = height * width;
                int chw = channels * hw;

                Console.WriteLine($"Batch info - Dimensions: {batch}x{channels}x{height}x{width}");

                for (int b = 0; b < batch; b++)
                {
                    var matStopwatch = Stopwatch.StartNew(); // Đo thời gian xử lý cho mỗi ảnh
                    var mat = new Mat(new Size(width, height), MatType.CV_8UC3);
                    var ptr = mat.DataPointer;

                    int batchOffset = b * chw;

                    for (int y = 0; y < height; y++)
                    {
                        int rowStart = y * width * 3;

                        for (int x = 0; x < width; x++)
                        {
                            int pixelIndex = y * width + x;

                            float r = span[batchOffset + 0 * hw + pixelIndex];
                            float g = span[batchOffset + 1 * hw + pixelIndex];
                            float bVal = span[batchOffset + 2 * hw + pixelIndex];

                            int idx = rowStart + x * 3;

                            ptr[idx + 0] = (byte)Math.Clamp((int)(bVal * 255.0f), 0, 255);
                            ptr[idx + 1] = (byte)Math.Clamp((int)(g * 255.0f), 0, 255);
                            ptr[idx + 2] = (byte)Math.Clamp((int)(r * 255.0f), 0, 255);
                        }
                    }

                    results.Add(mat);
                    mat.SaveImage(Path.Combine(@"C:\Users\Talon Ezio\Downloads\Compressed", $"{Guid.NewGuid()}.jpg"), new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
                    matStopwatch.Stop(); // Dừng stopwatch cho mỗi ảnh

                    Console.WriteLine($"Processed image {b + 1} in batch in {matStopwatch.ElapsedMilliseconds} ms");
                }

                batchStopwatch.Stop(); // Dừng stopwatch cho mỗi batch
                Console.WriteLine($"Processed batch in {batchStopwatch.ElapsedMilliseconds} ms");
            }

            stopwatch.Stop(); // Dừng stopwatch cho toàn bộ quá trình
            Console.WriteLine($"Total processing time for all batches: {stopwatch.ElapsedMilliseconds} ms");

            return results;
        }
    }
}
