using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DumbCrawler.Helpers
{
    public class MemoryDatabase<TKey, TValue> : IDatabase<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _data = new ConcurrentDictionary<TKey, TValue>();

        public void Clear()
        {
            _data.Clear();
        }

        public TValue Delete(TKey key)
        {
            TValue tmp;

            return _data.TryRemove(key, out tmp) ? tmp : default(TValue);
        }

        public bool Exists(TKey key)
        {
            return _data.ContainsKey(key);
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TValue, TValue> update)
        {
            return _data.AddOrUpdate(key, addKey => addValue, (updateKey, value) => update(value));
        }

        public IQueryable<KeyValuePair<TKey, TValue>> AsQueryable()
        {
            return _data.AsQueryable();
        }

        public IDictionary<TKey, TValue> ToDictionary()
        {
            return _data.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public async Task MergeAsync(IDictionary<TKey, TValue> other, Func<TValue, TValue, TValue> update)
        {
            await Task.Run(() =>
            {
                foreach (var record in other)
                {
                    AddOrUpdate(record.Key, record.Value, value => update(value, record.Value));
                }
            });
        }

        public long Count()
        {
            return _data.Count;
        }
    }
}