using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ropu.Shared.AsyncTools
{
    public static class AsyncPump
    {
        public static void Run(Func<Task> func)
        {
            var prevCtx = SynchronizationContext.Current;

            try
            {
                var syncCtx = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);
                var t = func();

                t.ContinueWith(task => syncCtx.Complete(), TaskScheduler.Default);

                syncCtx.RunOnCurrentThread();

                t.GetAwaiter().GetResult();
            }
            finally 
            { 
                SynchronizationContext.SetSynchronizationContext(prevCtx); 
            }
        }
    }
}