using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;

namespace IServiceOriented.ServiceBus.Threading
{
    /// <summary>
    /// Used for countdown events. When the count reaches zero, the event will be set.
    /// </summary>
    public class CountdownLatch : IDisposable
    {
        public CountdownLatch(int count)
        {
            _count = count;
        }

        int _count;
        /// <summary>
        /// Gets the current count of the countdown
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }
        ManualResetEvent _handle = new ManualResetEvent(false);

        /// <summary>
        /// Decrement the count. Set the event to signalled if the count is zero;
        /// </summary>
        /// <returns>The new count</returns>
        public int Tick()
        {
            if (_disposed) throw new ObjectDisposedException("CoundownLatch");
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
                if (_disposed) throw new ObjectDisposedException("CoundownLatch");
                return _handle;
            }
        }

        volatile bool _disposed;

        /// <summary>
        /// Reset the count and set the event to non signalled.
        /// </summary>
        public void Reset(int count)
        {
            if (_disposed) throw new ObjectDisposedException("CoundownLatch");
            
            _count = count;
            _handle.Reset();
        }

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

        ~CountdownLatch()
        {
            Dispose(false);
        }
    }
}
