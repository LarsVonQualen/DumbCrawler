using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace DumbCrawler
{
    public class Requester
    {
        private readonly Parser _parser = new Parser();
        private readonly Database _database = new Database();
        private readonly Database _masterDatabase;
        private readonly Timer _timer = new Timer(1000);
        private readonly string _name;

        public event ErrorEventHandler OnError;

        public Requester(string name, Database database)
        {
            _masterDatabase = database;
            _name = name;

            _parser.OnError += (sender, args) => OnError?.Invoke(sender, args);

            _timer.Elapsed += async (sender, args) =>
            {
                if (_database.TotalCount() <= 0) return;

                IDictionary<Uri, long> dump;

                lock (_database)
                {
                    dump = _database.Dump();
                }

                await _masterDatabase.Merge(dump);
            };

            _timer.Start();
        }

        public async Task<IEnumerable<Uri>> ProcessAsync(Uri uri)
        {
            lock (_database)
            {
                if (_database.Exists(uri))
                {
                    _database.Update(uri);

                    Error("Requester: Url already visited");

                    return null;
                }

                _database.Add(uri);
            }

            var req = new HttpRequest(uri);
            WebResponse res;

            try
            {
                res = await req.ProcessAsync();
            }
            catch (Exception e)
            {
                Error($"Requester: Request error ({uri}) ({e.Message})", e);
                return null;
            }    

            return await _parser.ProcessAsync(res);
        }

        public IEnumerable<Uri> Process(List<Uri> urls)
        {
            var allUrls = new ConcurrentStack<Uri>();

            urls.AsParallel().WithDegreeOfParallelism(8).ForAll(uri =>
            {
                lock (_database)
                {
                    if (_database.Exists(uri))
                    {
                        _database.Update(uri);

                        Error("Requester: Url already visited");

                        return;
                    }

                    _database.Add(uri);
                }

                var req = new HttpRequest(uri);
                WebResponse res;

                try
                {
                    res = req.Process();
                }
                catch (Exception e)
                {
                    Error($"Requester: Request error ({uri}) ({e.Message})", e);
                    return;
                }

                var localUrls = _parser.Process(res);

                if (localUrls != null) allUrls.PushRange(localUrls.ToArray());
            });

            return allUrls.ToList();
        }

        private void Error(string message, Exception e = null)
        {
            OnError?.Invoke(this, new ErrorEventArgs(new Exception(message, e)));
        }
    }
}