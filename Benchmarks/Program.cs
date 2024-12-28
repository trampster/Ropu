
using BenchmarkDotNet.Running;
using Benchmarks;

var summary = BenchmarkRunner.Run<GroupSendBenchmark>();

// var benchmark = new GroupSendBenchmark();
// while (true)
// {
//     Console.WriteLine("1");
//     benchmark.SendToParrallelDedicatedThreads();
//     Console.WriteLine("2");
//     benchmark.SendToParrallelDedicatedThreads();
//     Console.WriteLine("3");
//     benchmark.SendToParrallelDedicatedThreads();
//     Console.WriteLine("4");
//     benchmark.SendToParrallelDedicatedThreads();
//     Console.WriteLine("5");
//     benchmark.SendToParrallelDedicatedThreads();
//     Console.WriteLine("6");
//     benchmark.SendToParrallelDedicatedThreads();
//     Console.WriteLine("7");
//     benchmark.SendToParrallelDedicatedThreads();
// }

// benchmark.Cleanup();
// Console.WriteLine("Cleanup");

// var foramtter = new TryFormatLongBenchmark();

// foramtter.TryFormatCountDigits(123, 0);


// ManualResetEvent manualResetEvent = new ManualResetEvent(false);

// ThreadPool.QueueUserWorkItem(UserWorkItem, null);
// manualResetEvent.WaitOne();
// manualResetEvent.Reset();

// ThreadPool.QueueUserWorkItem(UserWorkItem, null);
// manualResetEvent.WaitOne();
// manualResetEvent.Reset();

// ThreadPool.QueueUserWorkItem(UserWorkItem, null);
// manualResetEvent.WaitOne();
// manualResetEvent.Reset();

// var bytes = GC.GetTotalAllocatedBytes();
// ThreadPool.QueueUserWorkItem(UserWorkItem, null);

// manualResetEvent.WaitOne();

// var after = GC.GetTotalAllocatedBytes();

// Console.WriteLine(after - bytes);


// void UserWorkItem(object? sender)
// {
//     manualResetEvent.Set();
// }