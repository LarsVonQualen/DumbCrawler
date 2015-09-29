using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace DumbCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var errors = new List<ErrorEventArgs>();
                var timer = new Timer(1000);
                var database = new Database();
                Uri lastUri = new Uri("http://www.example.com");
                long lastEnqueueCount = 0;

                var feeder = new CrawlerUrlFeeder(args.Skip(1).Select(s => new Uri(s)), database, int.Parse(args[0]));

                feeder.OnError += (sender, eventArgs) => errors.Add(eventArgs);
                feeder.OnProcessed += (sender, uri) => lastUri = uri;
                feeder.OnEnqueue += (sender, l) => lastEnqueueCount = l;

                var lastCount = (long)0;

                timer.Elapsed += (sender, eventArgs) =>
                {
                    var count = database.DistinctCount();
                    var totalCount = database.TotalCount();
                    var top10 = database.Top10();
                    var throughPut = count - lastCount;

                    lastCount = count;

                    Console.Clear();
                    Console.WriteLine($"Throughput:\t{throughPut} req/s");
                    Console.WriteLine($"Distinct Count:\t{count}");
                    Console.WriteLine($"Total Count:\t{totalCount}");
                    Console.WriteLine($"Last Url:\t{lastUri}");
                    Console.WriteLine($"Queue Count:\t{lastEnqueueCount}");
                    Console.WriteLine($"Worker count:\t{feeder.GetCurrentWorkerCount() + 1}");
                    Console.WriteLine();
                    Console.WriteLine("Top10:");

                    foreach (var l in top10)
                    {
                        Console.WriteLine($" ({l.Value})\t\t{l.Key}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("Error stats:");

                    errors
                        .GroupBy(errorEventArgs => errorEventArgs.GetException().Message)
                        .OrderByDescending(argses => argses.Count())
                        .Take(5)
                        .Select(argses => new
                        {
                            Count = argses.Count(),
                            Exception = argses.ElementAt(0).GetException()
                        })
                        .ToList()
                        .ForEach(obj => Console.WriteLine($" ({obj.Count})\t\t{obj.Exception.Message}"));
                };

                feeder.OnStart += (sender, eventArgs) => timer.Start();

                feeder.Start();
                Console.ReadLine();
                feeder.Abort();
            }
            catch (Exception e)
            {                
                throw;
            }
        }
    }
}
