using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace IServiceOriented.ServiceBus.Threading
{    
    public sealed class WorkerThreads : IDisposable
    {        
        public WorkerThreads(TimeSpan timeout, Action<TimeSpan, object> unitOfWork)
        {
            UnitOfWork = unitOfWork;
            Timeout = timeout;
        }

        private readonly TimeSpan Timeout;
        private readonly Action<TimeSpan, object> UnitOfWork;

        public int Count
        {
            get
            {
                lock (_workers)
                {
                    return _workers.Count;
                }
            }
        }
        class WorkerInfo
        {
            public AutoResetEvent StopEvent;
            public volatile bool Stopping;
            public Thread Thread;
            public object State;
        }

        void worker(object param)
        {
            WorkerInfo info = (WorkerInfo)param;

            try
            {
                while (!info.Stopping)
                {
                    UnitOfWork(Timeout, info.State);
                }
            }
            finally
            {
                info.StopEvent.Set();
            }
        }


        object _workerLock = new Object();
        
        List<WorkerInfo> _workers = new List<WorkerInfo>();

        public int AddWorker()
        {
            return AddWorker(null);
        }

        public int AddWorker(object state)
        {
            lock (_workerLock)
            {
                if (_disposed) throw new ObjectDisposedException("WorkerThreads");

                WorkerInfo info = new WorkerInfo()
                {
                    Thread = new Thread(worker) { IsBackground = true },
                    Stopping = false,
                    StopEvent = new AutoResetEvent(false),
                    State = state
                };
                _workers.Add(info);
                info.Thread.Start(info);
                return _workers.Count - 1;
            }
        }

        public void AddWorkers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                AddWorkers(count);
            }
        }

        public void RemoveWorker(int index)
        {
            lock (_workerLock)
            {
                if (_disposed) throw new ObjectDisposedException("WorkerThreads");

                _workers[index].Stopping = true;
                if (!_workers[index].StopEvent.WaitOne(Timeout))
                {
                    _workers[index].Thread.Abort();
                }
                _workers.RemoveAt(index);
            }
        }

        public void RemoveAll()
        {
            lock (_workerLock)
            {
                if (_disposed) throw new ObjectDisposedException("WorkerThreads");

                for (int i = 0; i < _workers.Count; i++)
                {
                    _workers[i].Stopping = true;
                }
                if (!WaitHandle.WaitAll(_workers.Select(wi => wi.StopEvent).ToArray(), Timeout))
                {
                    foreach (WorkerInfo w in _workers)
                    {
                        if (w.Thread.ThreadState != ThreadState.Stopped)
                        {
                            w.Thread.Abort();
                        }
                    }
                }
                _workers.Clear();
            }
        }

        public void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_workerLock)
                    {
                        foreach (WorkerInfo w in _workers)
                        {
                            if (w.Thread.ThreadState != ThreadState.Stopped)
                            {
                                w.Thread.Abort();
                            }
                        }
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WorkerThreads()
        {
            Dispose(false);
        }

        private volatile bool _disposed;

    }
}
