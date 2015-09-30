using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DumbCrawler.Workers;

namespace DumbCrawler.Streams
{
    public class FunnelStream<TIn, TOut, TWorker> : BaseStream<TIn, TOut, TWorker> where TWorker : class, IWorker, new()
    {
        public IEnumerable<IStream<TIn>> ReturnFeeds { get; set; }

        protected override void Worker()
        {
            try
            {
                var result = ReturnFeeds?.SelectMany(stream => stream.Peek() ? stream.Next(1000) : new List<TIn>()).ToList();

                EnqueueRange(result);

                Thread.Yield();
            }
            catch (Exception e)
            {
                Error("FunnelStream", e);
            }
        }
    }
}