using System;
using System.IO;

namespace DumbCrawler.Workers
{
    public interface IWorker
    {
        void Start(Action work);
        void Stop();
        bool Running();
        event ErrorEventHandler OnError;
    }
}