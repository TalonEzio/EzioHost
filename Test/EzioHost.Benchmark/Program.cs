using System.Diagnostics;
using System.Text;

namespace EzioHost.Benchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            // Use an empty ManualConfig to prevent BenchmarkDotNet from attaching platform-specific diagnosers
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

            var sw = Stopwatch.StartNew();

            var x = new FfmpegBenchmarkGemini();

            x.Init();


            x.IterationSetup();
            sw.Restart();
            x.Process_RemuxToHls_Encode();
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");

            x.IterationSetup();
            sw.Restart();
            x.AutoGen_EncodeToHls();
            sw.Stop();
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            
            //x.IterationCleanup();
        }
    }
}
