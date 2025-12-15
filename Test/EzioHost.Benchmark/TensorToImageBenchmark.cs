using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace EzioHost.Benchmark;

[MemoryDiagnoser]
[ShortRunJob]
public class TensorToImageBenchmark
{
    private const string IMAGE_PATH = "input.jpg";
    private const string MODEL_PATH = "model.onnx";

    private static InferenceSession _session = null!;
    private Mat _image = null!; // input BGR
    private DenseTensor<float> _inputTensor = null!; // NCHW float32 [0..1]
    private float[] _onnxOutputData = null!;
    private int _outW, _outH;

    [GlobalSetup]
    public void Setup()
    {
        var opt = new SessionOptions();
        try
        {
            opt.AppendExecutionProvider_CUDA();
        }
        catch
        {
            /*ignore*/
        }

        _session = new InferenceSession(MODEL_PATH, opt);

        _image = Cv2.ImRead(IMAGE_PATH);

        _inputTensor = ImageToTensor_OpenCVBlob(_image);

        using var results = _session.Run([
            NamedOnnxValue.CreateFromTensor(_session.InputMetadata.Keys.First(), _inputTensor)
        ]);

        var outTensor = results.First().AsTensor<float>(); // [1,3,OH,OW]
        _outH = outTensor.Dimensions[2];
        _outW = outTensor.Dimensions[3];

        _onnxOutputData = new float[outTensor.Length];
        ((DenseTensor<float>)outTensor).Buffer.Span.CopyTo(_onnxOutputData);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _image?.Dispose();
        _session?.Dispose();
    }

    //================= BENCHMARKS =================

    [Benchmark(Baseline = true, Description = "Naive scalar (indexer 4D)")]
    public Mat TensorToImage_NaiveIndexer()
    {
        var t = new DenseTensor<float>(_onnxOutputData, new[] { 1, 3, _outH, _outW });
        return TensorToImage_Naive(t);
    }

    [Benchmark(Description = "Portable SIMD + Merge")]
    public Mat TensorToImage_PortableSimd()
    {
        return TensorToMatPortable(_onnxOutputData, _outW, _outH);
    }

    [Benchmark(Description = "AVX2 SIMD + Merge")]
    public Mat TensorToImage_Avx2()
    {
        return TensorToMatAvx2(_onnxOutputData, _outW, _outH);
    }

    //================= INPUT PATHS =================

    [Benchmark(Description = "Image -> Tensor (BlobFromImage)")]
    public DenseTensor<float> ImageToTensor_OpenCV()
    {
        return ImageToTensor_OpenCVBlob(_image);
    }

    [Benchmark(Description = "Image -> Tensor (unsafe pointer, stride-aware)")]
    public DenseTensor<float> ImageToTensor_Unsafe()
    {
        return ImageToTensor_FromMatUnsafe(_image);
    }

    //================= IMPLEMENTATION =================

    public DenseTensor<float> ImageToTensor_OpenCVBlob(Mat bgr)
    {
        using var blob = CvDnn.BlobFromImage(
            bgr,
            1.0 / 255.0,
            new Size(bgr.Width, bgr.Height),
            default,
            true, // BGR -> RGB
            false
        );
        var n = (int)blob.Total();
        var data = GC.AllocateUninitializedArray<float>(n);
        Marshal.Copy(blob.Data, data, 0, n);
        return new DenseTensor<float>(data, new[] { 1, 3, bgr.Height, bgr.Width });
    }

    private static unsafe DenseTensor<float> ImageToTensor_FromMatUnsafe(Mat bgr)
    {
        int w = bgr.Width, h = bgr.Height, hw = w * h;
        var data = GC.AllocateUninitializedArray<float>(3 * hw);

        var basePtr = bgr.DataPointer;
        var step = (int)bgr.Step(); // bytes per row
        int dstR = 0, dstG = hw, dstB = 2 * hw;

        for (var y = 0; y < h; y++)
        {
            var row = basePtr + y * step;
            for (var x = 0; x < w; x++)
            {
                var idx = x * 3;
                var b = row[idx + 0];
                var g = row[idx + 1];
                var r = row[idx + 2];

                var i = y * w + x;
                data[dstR + i] = r / 255f;
                data[dstG + i] = g / 255f;
                data[dstB + i] = b / 255f;
            }
        }

        return new DenseTensor<float>(data, new[] { 1, 3, h, w });
    }

    private static Mat TensorToImage_Naive(Tensor<float> t)
    {
        int h = t.Dimensions[2], w = t.Dimensions[3];
        var output = new Mat(new Size(w, h), MatType.CV_8UC3);

        unsafe
        {
            var dst = output.DataPointer;
            for (var y = 0; y < h; y++)
            {
                var row = y * w * 3;
                for (var x = 0; x < w; x++)
                {
                    var idx = row + x * 3;
                    var r = (int)(t[0, 0, y, x] * 255f);
                    var g = (int)(t[0, 1, y, x] * 255f);
                    var b = (int)(t[0, 2, y, x] * 255f);

                    // clamp thủ công
                    dst[idx + 2] = (byte)(r < 0 ? 0 : r > 255 ? 255 : r);
                    dst[idx + 1] = (byte)(g < 0 ? 0 : g > 255 ? 255 : g);
                    dst[idx + 0] = (byte)(b < 0 ? 0 : b > 255 ? 255 : b);
                }
            }
        }

        return output;
    }

    private static Mat TensorToMatPortable(ReadOnlySpan<float> nchw, int w, int h)
    {
        var hw = w * h;
        var rF = nchw[..hw];
        var gF = nchw.Slice(hw, hw);
        var bF = nchw.Slice(2 * hw, hw);

        var r = GC.AllocateUninitializedArray<byte>(hw);
        var g = GC.AllocateUninitializedArray<byte>(hw);
        var b = GC.AllocateUninitializedArray<byte>(hw);

        Float01ToBytesPortable(rF, r);
        Float01ToBytesPortable(gF, g);
        Float01ToBytesPortable(bF, b);

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

    private static void Float01ToBytesPortable(ReadOnlySpan<float> src, Span<byte> dst)
    {
        var n = src.Length;
        var vec = Vector<float>.Count;
        var vZero = Vector<float>.Zero;
        var vOne = new Vector<float>(1f);
        var v255 = new Vector<float>(255f);
        var i = 0;

        Span<float> tmp = stackalloc float[vec];

        for (; i <= n - vec; i += vec)
        {
            var vf = new Vector<float>(src.Slice(i, vec));
            vf = Vector.Min(Vector.Max(vf, vZero), vOne) * v255;
            vf.CopyTo(tmp);
            for (var k = 0; k < vec; k++) dst[i + k] = (byte)tmp[k];
        }

        for (; i < n; i++)
        {
            var x = src[i] * 255f;
            if (x < 0) x = 0;
            else if (x > 255f) x = 255f;
            dst[i] = (byte)x;
        }
    }

    private static Mat TensorToMatAvx2(ReadOnlySpan<float> nchw, int w, int h)
    {
        if (!Avx2.IsSupported)
            return TensorToMatPortable(nchw, w, h);

        var hw = w * h;
        var rF = nchw[..hw];
        var gF = nchw.Slice(hw, hw);
        var bF = nchw.Slice(2 * hw, hw);

        var r = GC.AllocateUninitializedArray<byte>(hw);
        var g = GC.AllocateUninitializedArray<byte>(hw);
        var b = GC.AllocateUninitializedArray<byte>(hw);

        Float01ToBytesAvx2(rF, r);
        Float01ToBytesAvx2(gF, g);
        Float01ToBytesAvx2(bF, b);

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
        if (!Avx2.IsSupported)
        {
            Float01ToBytesPortable(src, dst);
            return;
        }

        var n = src.Length;
        var i = 0;
        var vZero = Vector256<float>.Zero;
        var vOne = Vector256.Create(1f);
        var v255 = Vector256.Create(255f);

        fixed (float* pSrc = src)
        fixed (byte* pDst = dst)
        {
            // Mỗi vòng xử lý 16 phần tử float -> 16 byte
            for (; i <= n - 16; i += 16)
            {
                var v0 = Avx.LoadVector256(pSrc + i + 0);
                var v1 = Avx.LoadVector256(pSrc + i + 8);

                v0 = Avx.Min(Avx.Max(v0, vZero), vOne);
                v1 = Avx.Min(Avx.Max(v1, vZero), vOne);

                v0 = Avx.Multiply(v0, v255);
                v1 = Avx.Multiply(v1, v255);

                // float -> int32
                var i320 = Avx.ConvertToVector256Int32(v0);
                var i321 = Avx.ConvertToVector256Int32(v1);

                // int32 (8 + 8) -> int16 (saturate)
                var i160 = Sse2.PackSignedSaturate(i320.GetLower(), i320.GetUpper());
                var i161 = Sse2.PackSignedSaturate(i321.GetLower(), i321.GetUpper());

                // int16 (8 + 8) -> byte(16) (unsigned saturate)
                var u8 = Sse2.PackUnsignedSaturate(i160, i161); // Vector128<byte>

                // ghi 16 byte ra dst
                Sse2.Store(pDst + i, u8);
            }

            // tail
            for (; i < n; i++)
            {
                var x = src[i] * 255f;
                if (x < 0) x = 0;
                else if (x > 255f) x = 255f;
                pDst[i] = (byte)x;
            }
        }
    }
}