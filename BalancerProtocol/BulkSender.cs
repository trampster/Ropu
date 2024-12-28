using System.Net;
using System.Net.Sockets;

namespace BalancerProtocol;

public class BulkSender
{
    readonly Thread[] _threads = new Thread[Environment.ProcessorCount];
    readonly AutoResetEvent[] _threadWakeupEvents = new AutoResetEvent[Environment.ProcessorCount];
    readonly ManualResetEvent _doneEvent = new(false);
    readonly Socket _socket;

    ReadOnlyMemory<byte> _buffer = new byte[0];
    ReadOnlyMemory<SocketAddress> _destinations = new SocketAddress[0];
    int _completedThreads = 0;
    int _threadNumber = 0;
    volatile bool _stopping = false;


    public BulkSender(Socket socket)
    {
        _socket = socket;
        for (int index = 0; index < _threads.Length; index++)
        {
            var autoResetEvent = new AutoResetEvent(false);
            var thread = new Thread(ThreadWorker);
            thread.Start(autoResetEvent);
            _threads[index] = thread;
            _threadWakeupEvents[index] = autoResetEvent;
        }
    }

    public void SendBulk(ReadOnlyMemory<byte> buffer, Memory<SocketAddress> destinations)
    {
        _buffer = buffer;
        _destinations = destinations;
        Interlocked.Exchange(ref _threadNumber, 0);
        Interlocked.Exchange(ref _completedThreads, 0);
        _doneEvent.Reset();
        foreach (var resetEvent in _threadWakeupEvents)
        {
            resetEvent.Set();
        }
        _doneEvent.WaitOne();
    }

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
            int numberOfThreads = _threads.Length;
            int numberPerThread = _buffer.Length / numberOfThreads;
            int start = (threadNumber - 1) * numberPerThread;
            for (int index = start; index < start + numberPerThread; index++)
            {
                try
                {
                    _socket.SendTo(_buffer.Span, SocketFlags.None, _destinations.Span[index]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Index issue: Index: {index}, ArrayLength: {_destinations.Length}, Start: {start}, Number Per Thread {numberPerThread}");
                    throw;
                }
            }
            OnThreadWorkDone();
        }
    }

    void OnThreadWorkDone()
    {
        int newAmmount = Interlocked.Increment(ref _completedThreads);
        if (newAmmount == _threads.Length)
        {
            _doneEvent.Set();
        }
    }
}