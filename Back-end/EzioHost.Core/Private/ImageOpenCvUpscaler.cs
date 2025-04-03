using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace EzioHost.Core.Private
{
    internal sealed class ImageOpenCvUpscaler
    {
        public Mat UpscaleImage(string imagePath, InferenceSession session, int scale)
        {
            var inputImageBgr = Cv2.ImRead(imagePath);
            var inputImage = new Mat();
            Cv2.CvtColor(inputImageBgr, inputImage, ColorConversionCodes.BGR2RGB);

            var tensor = ImageToTensor(inputImage);

            var inputName = session.InputMetadata.Keys.First();
            var results = session.Run(new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
            var outputTensor = results.First().AsTensor<float>();

            var newWidth = inputImage.Width * scale;
            var newHeight = inputImage.Height * scale;

            var outputImage = TensorToImage(outputTensor, newWidth, newHeight);
            Cv2.CvtColor(outputImage, outputImage, ColorConversionCodes.RGB2BGR);
            return outputImage;
        }

        private static Tensor<float> ImageToTensor(Mat image)
        {
            var width = image.Width;
            var height = image.Height;

            var tensorData = new float[1 * 3 * height * width];//1D array

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = image.At<Vec3b>(y, x);

                    tensorData[0 * 3 * height * width + 0 * height * width + y * width + x] = pixel.Item0 / 255.0f;
                    tensorData[0 * 3 * height * width + 1 * height * width + y * width + x] = pixel.Item1 / 255.0f;
                    tensorData[0 * 3 * height * width + 2 * height * width + y * width + x] = pixel.Item2 / 255.0f;
                }
            }

            return new DenseTensor<float>(tensorData, new[] { 1, 3, height, width });
        }
        private static Mat TensorToImage(Tensor<float> tensor, int width, int height)
        {
            var output = new Mat(new Size(width, height), MatType.CV_8UC3);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var r = Math.Clamp((int)(tensor[0, 0, y, x] * 255), 0, 255);
                    var g = Math.Clamp((int)(tensor[0, 1, y, x] * 255), 0, 255);
                    var b = Math.Clamp((int)(tensor[0, 2, y, x] * 255), 0, 255);
                    output.Set(y, x, new Vec3b((byte)r, (byte)g, (byte)b));
                }
            }
            return output;
        }

    }
}
