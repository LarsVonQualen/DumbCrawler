using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DumbCrawler.Workers;

namespace DumbCrawler.Streams
{
    public class FeedStream<TWorker> : BaseStream<Uri, Uri, TWorker> where TWorker : class, IWorker, new()
    {
        public void Feed(IEnumerable<Uri> urls)
        {
            EnqueueRange(urls);
        }

        protected override void Worker()
        {
            try
            {
                while (!ReturnFeed.Peek())
                {
                    Thread.Yield();
                }

                var result = ReturnFeed.Next(1000);

                EnqueueRange(result);
            }
            catch (Exception e)
            {
                Error("FeedStream", e);
            }
        }
    }
}