using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Perfolizer.Mathematics.Common;

namespace Benchmarks;

[MemoryDiagnoser]
public class GroupSendBenchmark
{
    readonly SocketAddress[] _addresses = new SocketAddress[2000];
    readonly IPEndPoint[] _endpoints = new IPEndPoint[2000];
    readonly SocketAsyncEventArgs[] _args = new SocketAsyncEventArgs[2000];
    byte[] _buffer = new byte[2000];

    List<Thread> _threads = new List<Thread>();
    List<AutoResetEvent> _threadWakeupEvents = new List<AutoResetEvent>();


    readonly Socket _socket;

    public GroupSendBenchmark()
    {
        var address = IPAddress.Parse("192.168.1.115");
        for (int index = 0; index < _addresses.Length; index++)
        {
            _endpoints[index] = new IPEndPoint(address, index + 2000);
            _addresses[index] = _endpoints[index].Serialize();
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(_buffer, 0, _buffer.Length);
            args.RemoteEndPoint = _endpoints[index];
            _args[index] = args;
        }
        _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, 1999));

        var processorCount = Environment.ProcessorCount;
        for (int index = 0; index < processorCount; index++)
        {
            var autoResetEvent = new AutoResetEvent(false);
            var thread = new Thread(ThreadWorker);
            thread.Start(autoResetEvent);
            _threads.Add(thread);
            _threadWakeupEvents.Add(autoResetEvent);
        }
    }

    volatile bool _stopping = false;

    [GlobalCleanup]
    public void Cleanup()
    {
        _stopping = true;
        foreach (var resetEvent in _threadWakeupEvents)
        {
            resetEvent.Set();
        }
        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }

    [Benchmark]
    public int SendTo()
    {
        int written = 0;
        for (int index = 0; index < _addresses.Length; index++)
        {
            written += _socket.SendTo(_buffer, SocketFlags.None, _addresses[index]);
        }
        return written;
    }

    void SendToPart1(object? state)
    {
        for (int index = 0; index < 500; index++)
        {
            _socket.SendTo(_buffer, SocketFlags.None, _addresses[index]);
        }
        Done();
    }

    void SendToPart2(object? state)
    {
        for (int index = 500; index < 1000; index++)
        {
            _socket.SendTo(_buffer, SocketFlags.None, _addresses[index]);
        }
        Done();
    }

    void SendToPart3(object? state)
    {
        for (int index = 1000; index < 1500; index++)
        {
            _socket.SendTo(_buffer, SocketFlags.None, _addresses[index]);
        }
        Done();
    }

    void SendToPart4(object? state)
    {
        for (int index = 1500; index < 2000; index++)
        {
            _socket.SendTo(_buffer, SocketFlags.None, _addresses[index]);
        }
        Done();
    }

    int _sum = 0;

    void Done()
    {
        int newAmmount = Interlocked.Increment(ref _sum);
        if (newAmmount == 4)
        {
            _doneEvent.Set();
        }
    }

    ManualResetEvent _doneEvent = new ManualResetEvent(false);

    [Benchmark]
    public int SendToParrallelThreadPool()
    {
        Interlocked.Exchange(ref _sum, 0);
        _doneEvent.Reset();
        ThreadPool.QueueUserWorkItem(SendToPart1, null);
        ThreadPool.QueueUserWorkItem(SendToPart2, null);
        ThreadPool.QueueUserWorkItem(SendToPart3, null);
        ThreadPool.QueueUserWorkItem(SendToPart4, null);
        _doneEvent.WaitOne();
        return 0;
    }

    int _threadNumber = 0;

    void ThreadWorker(object? state)
    {
        AutoResetEvent resetEvent = (AutoResetEvent)state!;
        while (!_stopping)
        {
            resetEvent.WaitOne();
            if (_stopping)
            {
                return;
            }
            int threadNumber = Interlocked.Increment(ref _threadNumber);
            int numberOfThreads = _threads.Count;
            int numberPerThread = _buffer.Length / numberOfThreads;
            int start = (threadNumber - 1) * numberPerThread;
            for (int index = start; index < start + numberPerThread; index++)
            {
                try
                {
                    _socket.SendTo(_buffer, SocketFlags.None, _addresses[index]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Index issue: Index: {index}, ArrayLength: {_addresses.Length}, Start: {start}, Number Per Thread {numberPerThread}");
                    throw;
                }
            }
            DoneThreads();
        }
    }

    void DoneThreads()
    {
        int newAmmount = Interlocked.Increment(ref _sum);
        if (newAmmount == _threads.Count)
        {
            _doneEvent.Set();
        }
    }

    [Benchmark]
    public int SendToParrallelDedicatedThreads()
    {
        Interlocked.Exchange(ref _threadNumber, 0);
        Interlocked.Exchange(ref _sum, 0);
        _doneEvent.Reset();
        foreach (var resetEvent in _threadWakeupEvents)
        {
            resetEvent.Set();
        }
        _doneEvent.WaitOne();
        return _sum;
    }

    [Benchmark]
    public int SendToParrallel()
    {
        int written = 0;
        Parallel.For(0, _addresses.Length, index =>
        {
            written += _socket.SendTo(_buffer, SocketFlags.None, _addresses[index]);
        });
        return written;
    }

    readonly List<Task<int>> _tasks = new(2000);


    // [Benchmark]
    // public async Task<int> SendToAsyncValueTask()
    // {
    //     int written = 0;
    //     for (int index = 0; index < _addresses.Length; index++)
    //     {
    //         var valueTask = _socket.SendToAsync(_buffer, SocketFlags.None, _addresses[index]);
    //         if (valueTask.IsCompleted)
    //         {
    //             written += valueTask.Result;

    //         }
    //         else
    //         {
    //             _tasks.Add(valueTask.AsTask());
    //         }
    //     }
    //     foreach (var task in _tasks)
    //     {
    //         written += await task;
    //     }
    //     return written;
    // }

    // [Benchmark]
    // public int SendToAsyncArgs()
    // {
    //     int written = 0;
    //     for (int index = 0; index < _args.Length; index++)
    //     {
    //         if (_socket.SendToAsync(_args[index]))
    //         {
    //         }
    //         else
    //         {
    //             written += _args[index].BytesTransferred;
    //         }
    //     }
    //     return written;
    // }
}