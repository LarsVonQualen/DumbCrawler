using System;
using System.Threading;

namespace DumbCrawler.Workers
{
    public class ThreadedWorker : Worker
    {
        private Thread _worker;

        public override void Start(Action work)
        {
            _worker = new Thread(() => base.Start(work));
            _worker.Start();
        }

        public override void Stop()
        {
            base.Stop();
            _worker?.Abort();
        }
    }
}