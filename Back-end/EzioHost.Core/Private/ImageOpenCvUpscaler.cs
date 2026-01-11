using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace EzioHost.Core.Private;

internal sealed class ImageOpenCvUpscaler
{
    public Task<Mat> UpscaleImageAsync(string imagePath, InferenceSession session, int scale)
    {
        return Task.Run(() => UpscaleImage(imagePath, session, scale));
    }

    public Mat UpscaleImage(string imagePath, InferenceSession session, int scale)
    {
        var stopwatch = Stopwatch.StartNew();

        var inputImage = Cv2.ImRead(imagePath);
        //Cv2.CvtColor(inputImage, inputImage, ColorConversionCodes.BGR2RGB);

        var inputName = session.InputMetadata.Keys.First();
        // Check if the model expects FP16 input
        var inputType = session.InputMetadata[inputName].ElementDataType;

        IDisposableReadOnlyCollection<DisposableNamedOnnxValue>? results = null;

        if (inputType == TensorElementType.Float16)
        {
            // Create FP16 batch tensor: [batch_size, 3, H, W]
            var batchTensorFp16 = ImageToTensorFp16(inputImage);
            results = session.Run([NamedOnnxValue.CreateFromTensor(inputName, batchTensorFp16)]);
        }
        else if (inputType == TensorElementType.Float)
        {
            // Fallback to FP32 if model doesn't support FP16
            var batchTensor = ImageToTensorFp32(inputImage);
            results = session.Run([NamedOnnxValue.CreateFromTensor(inputName, batchTensor)]);
        }

        if (results == null)
            throw new NotSupportedException(
                $"Model input type {inputType} is not supported. Only Float16 and Float32 are supported.");
        stopwatch.Stop();

        var outputTensor = results.First().AsTensor<float>();

        stopwatch.Restart();
        var outputImage = TensorToImage(outputTensor);
        stopwatch.Stop();


        return outputImage;
    }

    private static DenseTensor<float> ImageToTensorFp32(Mat image)
    {
        using var blob = CvDnn.BlobFromImage(
            image,
            1.0 / 255.0,
            new Size(image.Width, image.Height),
            default,
            true,
            false
        );

        var n = (int)blob.Total();
        var data = GC.AllocateUninitializedArray<float>(n);
        Marshal.Copy(blob.Data, data, 0, n);
        return new DenseTensor<float>(data, [1, 3, image.Height, image.Width]);
    }


    private static DenseTensor<Float16> ImageToTensorFp16(Mat image)
    {
        using var blob = CvDnn.BlobFromImage(
            image,
            1.0 / 255.0,
            new Size(image.Width, image.Height),
            default,
            true,
            false
        );

        var n = (int)blob.Total();

        var floatData = GC.AllocateUninitializedArray<float>(n);
        Marshal.Copy(blob.Data, floatData, 0, n);

        var fp16Data = GC.AllocateUninitializedArray<Float16>(n);

        Parallel.For(0, n, i => { fp16Data[i] = (Float16)floatData[i]; });

        // 5. Tạo Tensor
        return new DenseTensor<Float16>(fp16Data, [1, 3, image.Height, image.Width]);
    }

    private static Mat TensorToImage(Tensor<float> tensor)
    {
        var dense = (DenseTensor<float>)tensor;
        ReadOnlySpan<float> buf = dense.Buffer.Span; // NCHW
        int h = tensor.Dimensions[2], w = tensor.Dimensions[3];
        var hw = w * h;

        var rF = buf[..hw];
        var gF = buf.Slice(hw, hw);
        var bF = buf.Slice(2 * hw, hw);

        var r = GC.AllocateUninitializedArray<byte>(hw);
        var g = GC.AllocateUninitializedArray<byte>(hw);
        var b = GC.AllocateUninitializedArray<byte>(hw);

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

                var u8 = Sse2.PackUnsignedSaturate(i16A, i16B); // 16 byte
                Sse2.Store(pDst + i, u8);
            }

            for (; i < n; i++)
            {
                var x = src[i] * 255f;
                if (x < 0) x = 0;
                else if (x > 255f) x = 255f;
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
            for (var k = 0; k < v; k++) dst[i + k] = (byte)tmp[k];
        }

        for (; i < n; i++)
        {
            var x = src[i] * 255f;
            if (x < 0) x = 0;
            else if (x > 255f) x = 255f;
            dst[i] = (byte)x;
        }
    }
}