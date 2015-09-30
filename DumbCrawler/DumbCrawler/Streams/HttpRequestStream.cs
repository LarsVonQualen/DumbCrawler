using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using DumbCrawler.Helpers;
using DumbCrawler.Workers;

namespace DumbCrawler.Streams
{
    public class HttpRequestStream<TWorker> : BaseStream<ContentLoad, Uri, TWorker> where TWorker : class, IWorker, new()
    {
        private readonly IDatabase<Uri, long> _database;

        public HttpRequestStream(IDatabase<Uri, long> database)
        {
            _database = database;
        }

        protected override void Worker()
        {
            try
            {
                while (!ReturnFeed.Peek())
                {
                    Thread.Yield();
                }

                var urls = ReturnFeed.Next(10).ToList();
                var newUrls = urls.Where(url => !_database.Exists(url)).ToList();

                var contentLoads = newUrls.Select(Request).ToList();

                newUrls.ForEach(url => _database.AddOrUpdate(url, 1, l => l));
                urls.Where(url => _database.Exists(url)).ToList().ForEach(url => _database.AddOrUpdate(url, 1, l => l + 1));

                EnqueueRange(contentLoads);
            }
            catch (Exception e)
            {
                Error("HttpRequestStream", e);
            }
        }

        private ContentLoad Request(Uri url)
        {
            var client = new WebClient();
            string content;

            try
            {
                content = client.DownloadString(url);
            }
            catch (WebException e)
            {
                Error("HttpRequestStream Request", e);
                content = string.Empty;
            }

            return new ContentLoad()
            {
                Url = url,
                Content = content
            };
        }
    }
}