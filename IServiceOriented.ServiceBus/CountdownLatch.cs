using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace IServiceOriented.ServiceBus
{
    public class CountdownLatch : IDisposable
    {
        public CountdownLatch(int count)
        {
            _count = count;
        }

        int _count;
        ManualResetEvent _handle = new ManualResetEvent(false);

        public int Tick()
        {
            int count = Interlocked.Decrement(ref _count);
            if (count == 0)
            {
                _handle.Set();
            }
            return count;
        }

        public WaitHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        volatile bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_handle != null)
                {
                    ((IDisposable)_handle).Dispose();
                    _handle = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
    }
}
