using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DumbCrawler.Helpers
{
    public class UriDatabase : MemoryDatabase<Uri, long>
    {
        public long DistinctCount()
        {
            return base.Count();
        }

        public long TotalCount()
        {
            return base.AsQueryable().ToList().Sum(pair => pair.Value);
        }

        public IDictionary<Uri, long> Top10()
        {
            return base.AsQueryable().OrderByDescending(pair => pair.Value).Take(10).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public IDictionary<Uri, long> Dump()
        {
            var copy = new Dictionary<Uri, long>(SoftDump());

            base.Clear();

            return copy;
        }

        public IDictionary<Uri, long> SoftDump()
        {
            var dump = base.ToDictionary();

            return dump;
        }

        public async Task Merge(IDictionary<Uri, long> urls)
        {
            await base.MergeAsync(urls, (l, l1) => l + l1);
        }
    }
}