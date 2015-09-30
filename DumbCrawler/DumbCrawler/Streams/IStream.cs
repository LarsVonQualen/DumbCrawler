using System.Collections.Generic;

namespace DumbCrawler.Streams
{
    public interface IStream<out TValue>
    {
        void Start();
        void Stop();
        bool Peek();
        IEnumerable<TValue> Next(int maxElements);
    }
}