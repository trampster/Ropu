using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser(false)]
public class TryFormatBenchmarks
{
    private static readonly char[] _buffer = new char[1024];

    [Benchmark]
    [Arguments(13456)]
    public int TryFormat(long value)
    {
        _ = value.TryFormat(_buffer.AsSpan(), out int written);
        return _buffer[1];
    }
}