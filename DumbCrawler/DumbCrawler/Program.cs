using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DumbCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var app = new Application();

                app.OnRequest += (sender, d) =>
                {
                    if (d["success"] % 10 == 0)
                    {
                        Console.Clear();
                        Console.WriteLine("Requested {0} urls successfully and {1} failed", d["success"], d["failed"]);

                        var totalPlaces = app.GetPlaces().Distinct().ToList();
                        var errors = app.GetErrors().OrderByDescending(pair => pair.Value);
                        var top10 = app.GetTop10();

                        Console.WriteLine("Visited {0} distinct urls.", totalPlaces.Count);
                        Console.WriteLine();
                        Console.WriteLine("Error stats:");

                        foreach (var error in errors)
                        {
                            Console.WriteLine("   {0}\t{1}", error.Value, error.Key);
                        }

                        Console.WriteLine();
                        Console.WriteLine("Top 10:");
                        top10.ForEach(result => Console.WriteLine(string.Join("", $"({result.Count})\t{result.Url}".Take(80))));
                    }
                };

                app.OnComplete += (sender, e) => Console.WriteLine("Done");
                app.OnExhaustian += (sender, e) => Console.WriteLine("Exhausted");
                app.OnStop += (sender, e) => Console.WriteLine("Stopped");

                Console.WriteLine(args);

                app.Run(args[0]);

                Console.ReadLine();

                app.Stop();

                Console.ReadLine();
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                //Console.ReadLine();
            }
        }
    }
}
