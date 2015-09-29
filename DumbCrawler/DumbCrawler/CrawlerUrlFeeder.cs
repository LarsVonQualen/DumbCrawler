using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DumbCrawler
{
    public class CrawlerUrlFeeder
    {
        private readonly object _currentRequesterLock = new object();
        private readonly object _keepWorkingLock = new object();
        private readonly object _workerCountLock = new object();
        private readonly object _currentWorkerCountLock = new object();

        private readonly ConcurrentQueue<Uri> _queue = new ConcurrentQueue<Uri>();
        private readonly List<Requester> _requesters;
        private readonly List<Uri> _initialUrls; 
        private readonly Database _database;

        private readonly int _workerCount;

        private int _currentRequester;
        private bool _keepWorking = true;
        private int _currentWorkerCount = 0;
        

        public DateTime Started { get; set; }

        public event ErrorEventHandler OnError;
        public event EventHandler OnStart;
        public event EventHandler<Uri> OnProcessed;
        public event EventHandler<long> OnEnqueue;

        public CrawlerUrlFeeder(IEnumerable<Uri> initialUrls, Database database, int workerCount = 4)
        {
            _initialUrls = initialUrls.ToList();
            _workerCount = workerCount;
            _database = database;
            _requesters = new List<Requester>();

            for (int i = 0; i < _workerCount; i++)
            {
                var requester = new Requester($"Requester #{i}", _database);

                requester.OnError += (sender, args) => OnError?.Invoke(sender, args);

                _requesters.Add(requester);
            }

            EnqueueRange(_initialUrls);
        }

        public void Start()
        {
            Started = DateTime.Now;

            Console.Write($"Starting feeder with a maximum of '{_workerCount}' workers in... 5... ");
            Thread.Sleep(1000);
            Console.Write("4... ");
            Thread.Sleep(1000);
            Console.Write("3... ");
            Thread.Sleep(1000);
            Console.Write("2... ");
            Thread.Sleep(1000);
            Console.Write("1...");
            Thread.Sleep(1000);
            
            OnStart?.Invoke(this, EventArgs.Empty);

            Task.Run(() => DoWork(_currentWorkerCount));
        }

        public void Abort()
        {
            lock (_keepWorkingLock)
            {
                _keepWorking = false;
            }
        }

        private void EnqueueRange(IEnumerable<Uri> uris)
        {
            var urls = uris?.ToList();

            if (urls == null) return;

            OnEnqueue?.Invoke(this, _queue.Count + urls.Count);

            urls.ForEach(_queue.Enqueue);
        }

        private void DoWork(int number)
        {
            var requester = _requesters.ElementAtOrDefault(number);

            try
            {
                while (KeepWorking())
                {
                    var bufferSize = 100;
                    var current = 0;
                    var urls = new List<Uri>();
                    Uri url;

                    while (_queue.TryDequeue(out url) && current < bufferSize)
                    {
                        urls.Add(url);
                        current++;
                        Thread.Yield();
                    }

                    var uris = requester?.Process(urls).ToList();

                    EnqueueRange(uris);

                    uris?.ForEach(uri => OnProcessed?.Invoke(this, uri));

                    if (current == 100 && GetCurrentWorkerCount() < GetWorkerCount())
                    {
                        Task.Run(() => DoWork(IncrementCurrentWorkerCount()));
                    }

                    while (!_queue.TryPeek(out url))
                    {
                        Thread.Yield();
                    }
                }
            }
            catch (Exception e)
            {
                Error($"Feeder: Worker #{number} died ({e.Message})", e);

                Task.Run(() => DoWork(number));
            }
        }

        private int GetWorkerCount()
        {
            lock (_workerCountLock)
            {
                return _workerCount;
            }
        }

        public int GetCurrentWorkerCount()
        {
            lock (_currentWorkerCountLock)
            {
                return _currentWorkerCount;
            }
        }

        private int IncrementCurrentWorkerCount()
        {
            return Interlocked.Increment(ref _currentWorkerCount);
        }

        private bool KeepWorking()
        {
            lock (_keepWorkingLock)
            {
                return _keepWorking;
            }
        }

        private Requester NextRequester()
        {
            lock (_currentRequesterLock)
            {
                _currentRequester += 1;

                if (_currentRequester == _requesters.Count)
                {
                    _currentRequester = 0;
                }

                return _requesters.ElementAtOrDefault(_currentRequester);
            }
        }

        private void Error(string message, Exception e = null)
        {
            OnError?.Invoke(this, new ErrorEventArgs(new Exception(message, e)));
        }
    }
}