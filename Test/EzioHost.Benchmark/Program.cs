using OpenCvSharp;
using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Running;
using Microsoft.ML.OnnxRuntime;

namespace EzioHost.Benchmark
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
