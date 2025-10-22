using System.Net;
using System.Net.Sockets;

namespace Ropu.Protocol;

public class BulkSender
{
    readonly Thread[] _threads = new Thread[Environment.ProcessorCount];
    readonly AutoResetEvent[] _threadWakeupEvents = new AutoResetEvent[Environment.ProcessorCount];
    readonly ManualResetEvent _doneEvent = new(false);
    readonly Socket _socket;

    ReadOnlyMemory<byte> _buffer = new byte[0];
    ReadOnlyMemory<SocketAddress> _destinations = new SocketAddress[0];
    SocketAddress? _except = null;
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

    public void SendBulk(ReadOnlyMemory<byte> buffer, Memory<SocketAddress> destinations, SocketAddress? except)
    {
        _buffer = buffer;
        _destinations = destinations;
        _except = except;
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
        try
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
                int numberPerThread = _destinations.Span.Length / numberOfThreads;
                if (numberPerThread == 0)
                {
                    numberPerThread = 1;
                }

                int start = (threadNumber - 1) * numberPerThread;

                int end = start + numberPerThread - 1;
                if (end >= _destinations.Span.Length)
                {
                    end = _destinations.Span.Length - 1;
                }
                if (threadNumber == numberOfThreads)
                {
                    end = _destinations.Span.Length - 1;
                }

                for (int index = start; index <= end; index++)
                {
                    var address = _destinations.Span[index];
                    if (address == _except)
                    {
                        continue;
                    }
                    _socket.SendTo(_buffer.Span, SocketFlags.None, address);
                }

                OnThreadWorkDone();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Worker failed with error {exception}");
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