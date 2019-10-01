using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ropu.Shared
{
    public class Timer : IDisposable
    {
        public Timer()
        {
            Callback = () => {};
        }

        readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
        Task? _task;

        public int Duration
        {
            get;
            set;
        }

        public Action Callback
        {
            get;
            set;
        }

        public void Start()
        {
            _manualResetEvent.Reset();
            _task = Task.Run(() => 
            {
                if(!_manualResetEvent.WaitOne(Duration))
                {
                    Callback();
                }
            });
        }

        public void Cancel()
        {
            _manualResetEvent.Set();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cancel();
                _manualResetEvent.Dispose();
                _task?.Wait();
                _task?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}