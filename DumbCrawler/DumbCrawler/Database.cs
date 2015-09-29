using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DumbCrawler
{
    public class Database
    {
        private readonly ConcurrentDictionary<Uri, long> _urls = new ConcurrentDictionary<Uri, long>();  

        public void Add(Uri url)
        {
            _urls.TryAdd(url, 1);
        }

        public bool Exists(Uri url)
        {
            return _urls.ContainsKey(url);
        }

        public void Update(Uri url)
        {
            _urls.AddOrUpdate(url, uri => 1, (uri, l) => l + 1);
        }

        public void AddOrUpdate(Uri url, long count)
        {
            _urls.AddOrUpdate(url, uri => count, (uri, l) => l + count);
        }

        public long DistinctCount()
        {
            return _urls.Count;
        }

        public long TotalCount()
        {
            return _urls.Sum(pair => pair.Value);
        }

        public IDictionary<Uri, long> Top10()
        {
            return _urls.ToArray().OrderByDescending(pair => pair.Value).Take(10).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public IDictionary<Uri, long> Dump()
        {
            var copy = new Dictionary<Uri, long>(_urls.ToDictionary(pair => pair.Key, pair => pair.Value));

            _urls.Clear();

            return copy;
        }

        public async Task Merge(IDictionary<Uri, long> urls)
        {
            await Task.Run(() =>
            {
                foreach (var url in urls)
                {
                    AddOrUpdate(url.Key, url.Value);
                }
            });
        }
    }
}