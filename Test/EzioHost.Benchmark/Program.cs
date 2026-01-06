using System.Text;

namespace EzioHost.Benchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.Unicode;
        // Use an empty ManualConfig to prevent BenchmarkDotNet from attaching platform-specific diagnosers
        //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}