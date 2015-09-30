using System;
using System.IO;
using System.Threading;

namespace DumbCrawler.Workers
{
    public class Worker : IWorker
    {
        private long _running = 1;

        public event ErrorEventHandler OnError;

        public virtual void Start(Action work)
        {
            while (Running())
            {
                try
                {
                    work();
                }
                catch (Exception e)
                {
                    OnError?.Invoke(this, new ErrorEventArgs(new Exception($"Worker: {e.Message}", e)));
                }
            }
        }

        public virtual void Stop()
        {
            Interlocked.Decrement(ref _running);
        }

        public virtual bool Running()
        {
            return Interlocked.Read(ref _running) > 0;
        }
    }
}