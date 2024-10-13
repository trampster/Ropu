
using BenchmarkDotNet.Running;
using Benchmarks;

var summary = BenchmarkRunner.Run<CountDigitsBenchmark>();

// var foramtter = new TryFormatLongBenchmark();

// foramtter.TryFormatCountDigits(123, 0);