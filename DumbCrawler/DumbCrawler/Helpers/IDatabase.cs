using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DumbCrawler.Helpers
{
    public interface IDatabase<TKey, TValue>
    {
        void Clear();
        TValue Delete(TKey key);
        bool Exists(TKey key);
        TValue AddOrUpdate(TKey key, TValue add, Func<TValue, TValue> update);
        IQueryable<KeyValuePair<TKey, TValue>> AsQueryable();
        IDictionary<TKey, TValue> ToDictionary();
        Task MergeAsync(IDictionary<TKey, TValue> other, Func<TValue, TValue, TValue> update);
        long Count();
    }
}