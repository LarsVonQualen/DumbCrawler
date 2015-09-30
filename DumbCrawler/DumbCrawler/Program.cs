using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using DumbCrawler.Helpers;
using DumbCrawler.Streams;
using DumbCrawler.Workers;

namespace DumbCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var uriDatabase = new UriDatabase();
            var errorDatabase = new MemoryDatabase<string, long>();

            GenerateReport(uriDatabase.SoftDump(), errorDatabase.ToDictionary());

            var feeder = new FeedStream<ThreadedWorker>();

            feeder.Feed(args.Skip(1).Select(s => new Uri(s)));

            var requesters = new List<HttpRequestStream<ThreadedWorker>>();

            for (int i = 0; i < int.Parse(args.ElementAt(0)); i++)
            {
                requesters.Add(new HttpRequestStream<ThreadedWorker>(uriDatabase));
            }

            var funnel = new FunnelStream<ContentLoad, Uri, ThreadedWorker>();
            var parser = new ParserStream<ThreadedWorker>();

            feeder.ReturnFeed = parser;
            parser.ReturnFeed = funnel;
            
            requesters.ForEach(stream => stream.ReturnFeed = feeder);
            funnel.ReturnFeeds = requesters;

            feeder.OnError += (sender, eventArgs) => CollectError(errorDatabase, eventArgs.GetException());
            parser.OnError += (sender, eventArgs) => CollectError(errorDatabase, eventArgs.GetException());
            funnel.OnError += (sender, eventArgs) => CollectError(errorDatabase, eventArgs.GetException());
            requesters.ForEach(stream => stream.OnError += (sender, eventArgs) => CollectError(errorDatabase, eventArgs.GetException()));

            parser.Start();
            funnel.Start();
            requesters.ForEach(stream => stream.Start());
            feeder.Start();

            var timer = new Timer(2000);

            timer.Elapsed += (sender, eventArgs) =>
            {
                var dump = uriDatabase.SoftDump();
                var errors = errorDatabase.ToDictionary();

                GenerateReport(dump, errors);
            };

            timer.Start();

            Console.ReadLine();
        }

        static void GenerateReport(IDictionary<Uri, long> dump, IDictionary<string, long> errors)
        {
            var totalCount = dump.Sum(pair => pair.Value);
            var distinctCount = dump.Count;
            var errorCount = errors.Sum(pair => pair.Value);
            var top10 = dump.OrderByDescending(pair => pair.Value).Take(10).ToList();
            var top10Errors = errors.OrderByDescending(pair => pair.Value).Take(10).ToList();

            Console.Clear();

            Console.WriteLine($"Total Count:\t\t{totalCount}");
            Console.WriteLine($"Distinct Count:\t\t{distinctCount}");
            Console.WriteLine($"Error Count:\t\t{errorCount}");
            Console.WriteLine();
            Console.WriteLine("Top 10:");
            top10.ForEach(pair => Console.WriteLine($" ({pair.Value}) {pair.Key}"));Console.WriteLine();
            Console.WriteLine("Top 10 Errors:");
            top10Errors.ForEach(pair => Console.WriteLine($" ({pair.Value}) {pair.Key}"));
        }

        static void CollectError(MemoryDatabase<string, long> collection, Exception e)
        {
            collection.AddOrUpdate(e.Message, 1, l => l + 1);
        }
    }
}
