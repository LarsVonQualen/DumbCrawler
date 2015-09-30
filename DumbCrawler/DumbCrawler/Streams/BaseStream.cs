using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DumbCrawler.Workers;

namespace DumbCrawler.Streams
{
    public abstract class BaseStream<TValue, TReturn, TWorker> : IStream<TValue> where TWorker : class, IWorker, new()
    {
        protected readonly ConcurrentQueue<TValue> _queue = new ConcurrentQueue<TValue>();
        protected readonly IWorker _worker = new TWorker();
        protected readonly int _queueSize;

        public IStream<TReturn> ReturnFeed { get; set; }

        public event ErrorEventHandler OnError;

        protected BaseStream(int queueSize = 1000)
        {
            _queueSize = queueSize;

            _worker.OnError += (sender, args) => OnError?.Invoke(sender, args);
        }

        public virtual void Start()
        {
            _worker.Start(Worker);
        }

        public virtual void Stop()
        {
            _worker.Stop();
        }

        public virtual bool Peek()
        {
            return _queue.Any();
        }

        protected void EnqueueRange(IEnumerable<TValue> elements)
        {
            var enumeratedElements = elements?.ToList();

            //while (_queue.Count >= _queueSize || (_queue.Count + enumeratedElements?.Count) >= _queueSize)
            //{
            //    Thread.Yield();
            //}

            enumeratedElements?.ForEach(_queue.Enqueue);
        }

        public virtual IEnumerable<TValue> Next(int maxElements)
        {
            try
            {
                var result = new List<TValue>();

                TValue tmp;
                int current = 0;

                while (_queue.TryPeek(out tmp) && current++ < maxElements)
                {
                    if (_queue.TryDequeue(out tmp)) result.Add(tmp);
                }

                return result;
            }
            catch (Exception e)
            {
                Error("BaseStream", e);

                return new List<TValue>();
            }
        }

        protected abstract void Worker();

        protected void Error(string message, Exception e)
        {
            OnError?.Invoke(this, new ErrorEventArgs(new Exception($"{message}: {e.Message}", e)));
        }
    }
}