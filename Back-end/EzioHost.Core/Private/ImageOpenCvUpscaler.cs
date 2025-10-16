using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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
            //Cv2.CvtColor(inputImage, inputImage, ColorConversionCodes.BGR2RGB);

            var tensor = ImageToTensor(inputImage);

            stopwatch.Restart();
            var inputName = session.InputMetadata.Keys.First();
            var results = session.Run([NamedOnnxValue.CreateFromTensor(inputName, tensor)]);
            stopwatch.Stop();
            Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId} - Run inference: {stopwatch.ElapsedMilliseconds} ms");

            var outputTensor = results.First().AsTensor<float>();

            stopwatch.Restart();
            var outputImage = TensorToImageFast(outputTensor);
            stopwatch.Stop();
            Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId} - Convert tensor to image: {stopwatch.ElapsedMilliseconds} ms");

            return outputImage;
        }

        private static DenseTensor<float> ImageToTensor(Mat image)
        {
            using var blob = CvDnn.BlobFromImage(
                image: image,
                scaleFactor: 1.0 / 255.0,
                size: new Size(image.Width, image.Height),
                mean: default,
                swapRB: true,
                crop: false
            );

            var n = (int)blob.Total();
            var data = GC.AllocateUninitializedArray<float>(n);
            Marshal.Copy(blob.Data, data, 0, n);
            return new DenseTensor<float>(data, new[] { 1, 3, image.Height, image.Width });
        }

        private static Mat TensorToImageFast(Tensor<float> tensor)
        {
            var dense = (DenseTensor<float>)tensor;
            ReadOnlySpan<float> buf = dense.Buffer.Span;          // NCHW
            int h = tensor.Dimensions[2], w = tensor.Dimensions[3];
            int hw = w * h;

            var rF = buf[..hw];
            var gF = buf.Slice(hw, hw);
            var bF = buf.Slice(2 * hw, hw);

            byte[] r = GC.AllocateUninitializedArray<byte>(hw);
            byte[] g = GC.AllocateUninitializedArray<byte>(hw);
            byte[] b = GC.AllocateUninitializedArray<byte>(hw);

            if (Avx2.IsSupported)
            {
                Float01ToBytesAvx2(rF, r);
                Float01ToBytesAvx2(gF, g);
                Float01ToBytesAvx2(bF, b);
            }
            else
            {
                Float01ToBytesPortable(rF, r);
                Float01ToBytesPortable(gF, g);
                Float01ToBytesPortable(bF, b);
            }

            using var rMat = new Mat(h, w, MatType.CV_8UC1);
            using var gMat = new Mat(h, w, MatType.CV_8UC1);
            using var bMat = new Mat(h, w, MatType.CV_8UC1);
            Marshal.Copy(r, 0, rMat.Data, r.Length);
            Marshal.Copy(g, 0, gMat.Data, g.Length);
            Marshal.Copy(b, 0, bMat.Data, b.Length);

            var outMat = new Mat();
            Cv2.Merge([bMat, gMat, rMat], outMat);
            return outMat;
        }

        private static unsafe void Float01ToBytesAvx2(ReadOnlySpan<float> src, Span<byte> dst)
        {
            int n = src.Length, i = 0;
            var v0 = Vector256<float>.Zero;
            var v1 = Vector256.Create(1f);
            var v255 = Vector256.Create(255f);

            fixed (float* pSrc = src)
            fixed (byte* pDst = dst)
            {
                for (; i <= n - 16; i += 16)
                {
                    var a = Avx.LoadVector256(pSrc + i + 0);
                    var b = Avx.LoadVector256(pSrc + i + 8);

                    a = Avx.Min(Avx.Max(a, v0), v1);
                    b = Avx.Min(Avx.Max(b, v0), v1);
                    a = Avx.Multiply(a, v255);
                    b = Avx.Multiply(b, v255);

                    var i32A = Avx.ConvertToVector256Int32(a);
                    var i32B = Avx.ConvertToVector256Int32(b);

                    var i16A = Sse2.PackSignedSaturate(i32A.GetLower(), i32A.GetUpper());
                    var i16B = Sse2.PackSignedSaturate(i32B.GetLower(), i32B.GetUpper());

                    var u8 = Sse2.PackUnsignedSaturate(i16A, i16B);   // 16 byte
                    Sse2.Store(pDst + i, u8);
                }
                for (; i < n; i++)
                {
                    float x = src[i] * 255f;
                    if (x < 0) x = 0; else if (x > 255f) x = 255f;
                    pDst[i] = (byte)x;
                }
            }
        }

        private static void Float01ToBytesPortable(ReadOnlySpan<float> src, Span<byte> dst)
        {
            int n = src.Length, v = Vector<float>.Count, i = 0;
            var vz = Vector<float>.Zero;
            var v1 = new Vector<float>(1f);
            var v255 = new Vector<float>(255f);
            Span<float> tmp = stackalloc float[v];

            for (; i <= n - v; i += v)
            {
                var vf = new Vector<float>(src.Slice(i, v));
                vf = Vector.Min(Vector.Max(vf, vz), v1) * v255;
                vf.CopyTo(tmp);
                for (int k = 0; k < v; k++) dst[i + k] = (byte)tmp[k];
            }
            for (; i < n; i++)
            {
                float x = src[i] * 255f;
                if (x < 0) x = 0; else if (x > 255f) x = 255f;
                dst[i] = (byte)x;
            }
        }
    }
}
