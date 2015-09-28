using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace DumbCrawler
{
    public class Application
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.99 Safari/537.36";

        private readonly Uri _initialTarget = new Uri("https://youtube.com");
        private readonly Regex _isUri = new Regex("(https|http)://(.*)");
        private readonly ConcurrentBag<string> _places = new ConcurrentBag<string>();
        private readonly ConcurrentBag<string> _visited = new ConcurrentBag<string>();
        private readonly ConcurrentDictionary<string, long> _errors = new ConcurrentDictionary<string, long>();  

        private readonly object _runningFlagLock = new object();

        private bool _running = true;
        private long _reqCount = 0;
        private long _failedReqCount = 0;

        public event EventHandler<Dictionary<string, long>> OnRequest;
        public event EventHandler OnExhaustian;
        public event EventHandler OnStop;
        public event EventHandler OnComplete;

        public void Run(string url)
        {
            var firstPass = new List<string> {url};

            Task.Run(() =>
            {
                var lastPass = firstPass;
                
                while (KeepCrawling())
                {
                    if (!KeepCrawling())
                    {
                        break;
                    }

                    lastPass = lastPass.AsParallel().SelectMany(s => FindMatches(s).Result).ToList();

                    if (lastPass.Count == 0)
                    {
                        Stop();

                        OnExhaustian?.Invoke(this, EventArgs.Empty);

                        break;
                    };

                    lastPass.AsParallel().All(s => {
                        _places.Add(s);
                        return true;
                    });
                }

                OnComplete?.Invoke(this, EventArgs.Empty);
            });
        }

        public List<Top10Result> GetTop10()
        {
            return
                _places.ToList()
                    .GroupBy(s => s)
                    .OrderByDescending(grouping => grouping.Count())
                    .ThenBy(grouping => grouping.Key)
                    .Select(grouping => new Top10Result()
                    {
                        Url = grouping.Key,
                        Count = grouping.Count()
                    })
                    .Take(10)
                    .ToList();
        } 

        public List<string> GetPlaces()
        {
            return _visited.ToList();
        }

        public Dictionary<string, long> GetErrors()
        {
            return _errors.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public bool KeepCrawling()
        {
            lock (_runningFlagLock)
            {
                return _running;
            }
        }

        public void Stop()
        {
            lock (_runningFlagLock)
            {
                _running = false;
            }

            OnStop?.Invoke(this, EventArgs.Empty);
        }

        private async Task<IEnumerable<string>> FindMatches(string url)
        {
            try
            {
                if (_visited.Any(s => s.Equals(url)))
                {
                    Interlocked.Increment(ref _failedReqCount);
                    _errors.AddOrUpdate("Already Visited", 1, (status, l) => Interlocked.Increment(ref l));

                    return new List<string>();
                }
                else
                {
                    _visited.Add(url);
                }

                Uri actualUrl;

                try
                {
                    actualUrl = new Uri(url);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref _failedReqCount);
                    _errors.AddOrUpdate("Unparseable URI", 1, (status, l) => Interlocked.Increment(ref l));
                    return new List<string>();
                }

                var req = WebRequest.Create(actualUrl) as HttpWebRequest;

                if (req == null)
                {
                    Interlocked.Increment(ref _failedReqCount);
                    _errors.AddOrUpdate("Request Null", 1, (status, l) => Interlocked.Increment(ref l));
                    return new List<string>();
                }

                req.UserAgent = UserAgent;
                //req.Timeout = 1000;

                var response = await req.GetResponseAsync();

                if (!response.ContentType.ToLower().StartsWith(MediaTypeNames.Text.Html.ToLower()))
                {
                    Interlocked.Increment(ref _failedReqCount);
                    _errors.AddOrUpdate("Incompatible Content Type", 1, (status, l) => Interlocked.Increment(ref l));

                    return new List<string>();
                }

                Interlocked.Increment(ref _reqCount);

                await Task.Run(() => OnRequest?.Invoke(this, new Dictionary<string, long>
                {
                    {"success", Interlocked.Read(ref _reqCount)},
                    {"failed", Interlocked.Read(ref _failedReqCount)}
                }));

                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    try
                    {
                        var content = await stream.ReadToEndAsync();
                        var parsed = new HtmlDocument();
                        parsed.LoadHtml(content);

                        var matches = parsed.DocumentNode.SelectNodes("//a")
                            .Where(node => node != null)
                            .Select(node => node.GetAttributeValue("href", ""))
                            .Where(s => !string.IsNullOrEmpty(s) && (s.StartsWith("http") || s.StartsWith("https")) && _isUri.IsMatch(s));

                        return matches;
                    }
                    catch (Exception e)
                    {
                        Interlocked.Increment(ref _failedReqCount);
                        _errors.AddOrUpdate("parsing", 1, (status, l) => Interlocked.Increment(ref l));

                        return new List<string>();
                    }
                }
            }
            catch (WebException e)
            {
                Interlocked.Increment(ref _failedReqCount);

                _errors.AddOrUpdate(e.Status.ToString(), 1, (status, l) => Interlocked.Increment(ref l));

                return new List<string>();
            }
        }
    }

    public class Top10Result
    {
        public string Url { get; set; }
        public long Count { get; set; }
    }
}