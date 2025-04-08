using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Buffers;
using System.Diagnostics;

namespace EzioHost.Core.Private
{
    internal sealed class ImageOpenCvUpscaler
    {
        public Task<Mat> UpscaleImageAsync(string imagePath, InferenceSession session, int scale)
        {
            return Task.Run(() => UpscaleImage(imagePath, session, scale));
        }

        public Mat UpscaleImage(string imagePath, InferenceSession session, int scale)
        {
            var stopwatch = new Stopwatch();

            var inputImage = Cv2.ImRead(imagePath);
            Cv2.CvtColor(inputImage, inputImage, ColorConversionCodes.BGR2RGB);

            var tensor = ImageToTensor(inputImage);

            stopwatch.Restart();
            var inputName = session.InputMetadata.Keys.First();
            var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
            stopwatch.Stop();
            Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId} - Run inference: {stopwatch.ElapsedMilliseconds} ms");

            var outputTensor = results.First().AsTensor<float>();

            var newWidth = inputImage.Width * scale;
            var newHeight = inputImage.Height * scale;

            stopwatch.Restart();
            var outputImage = TensorToImage(outputTensor, newWidth, newHeight);
            stopwatch.Stop();
            Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId} - Convert tensor to image: {stopwatch.ElapsedMilliseconds} ms");

            return outputImage;
        }

        private static unsafe DenseTensor<float> ImageToTensor(Mat image)
        {
            int width = image.Width;
            int height = image.Height;
            int hw = height * width;
            var tensorData = ArrayPool<float>.Shared.Rent(3 * hw);

            byte* data = image.DataPointer;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width * 3;

                for (int x = 0; x < width; x++)
                {
                    int idx = rowOffset + x * 3;

                    byte r = data[idx];
                    byte g = data[idx + 1];
                    byte b = data[idx + 2];

                    int i = y * width + x;
                    tensorData[0 * hw + i] = r / 255.0f;
                    tensorData[1 * hw + i] = g / 255.0f;
                    tensorData[2 * hw + i] = b / 255.0f;
                }
            }

            return new DenseTensor<float>(tensorData.AsMemory(0, 3 * hw), new[] { 1, 3, height, width });

        }

        private static unsafe Mat TensorToImage(Tensor<float> tensor, int width, int height)
        {
            var output = new Mat(new Size(width, height), MatType.CV_8UC3);
            byte* data = output.DataPointer;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;

                for (int x = 0; x < width; x++)
                {
                    int i = rowOffset + x;
                    int idx = i * 3;

                    data[idx + 0] = (byte)Math.Clamp(tensor[0, 2, y, x] * 255f, 0, 255);
                    data[idx + 1] = (byte)Math.Clamp(tensor[0, 1, y, x] * 255f, 0, 255);
                    data[idx + 2] = (byte)Math.Clamp(tensor[0, 0, y, x] * 255f, 0, 255);
                }
            }

            return output;
        }
    }
}
