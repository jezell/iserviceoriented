using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;


namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Gaurds a value using ReaderWriteLockSlim 
    /// </summary>
    /// <typeparam name="TRead">Read only type of the contained data.</typeparam>
    /// <typeparam name="TWrite">Writeable type of the contained data.</typeparam>
    public class ReaderWriterLockedObject<TRead, TWrite> : ILockedObject<TRead, TWrite> where TRead : class
    {
        public ReaderWriterLockedObject(TWrite value, Converter<TWrite, TRead> makeReadOnly)
        {
            ReadOnlyConverter = makeReadOnly;
            _value = value;
            _readOnly = ReadOnlyConverter(_value);
        }

        public Converter<TWrite, TRead> ReadOnlyConverter
        {
            get;
            private set;
        }

        TWrite _value;
        ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #region ILockedObject<T> Members

        volatile TRead _readOnly;

        /// <summary>
        /// Read the contained value without acquiring a lock.
        /// </summary>
        /// <param name="action">Action to execute against read only object.</param>
        public void FastRead(Action<TRead> action)
        {
            if (_disposed) throw new ObjectDisposedException("ReaderWriteLockedObject");

            action(_readOnly);
        }

        /// <summary>
        /// Acquire a shared read lock and read the contained value.
        /// </summary>
        /// <param name="action">Action to execute against read only object.</param>
        public void Read(Action<TRead> action)
        {
            if (_disposed) throw new ObjectDisposedException("ReaderWriteLockedObject");

            _rwLock.EnterReadLock();
            try
            {
                action(_readOnly);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Acquire an exclusive write lock and write to the contained value.
        /// </summary>
        /// <param name="action">Action to execute against writeable object.</param>
        public void Write(Action<TWrite> action)
        {
            if (_disposed) throw new ObjectDisposedException("ReaderWriteLockedObject");

            _rwLock.EnterWriteLock();
            try
            {
                action(_value);
            }
            finally
            {
                // Make sure to update read only copy
                try
                {
                    _readOnly = ReadOnlyConverter(_value);
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
        }

        public void SetValue(TWrite value)
        {
            if (_disposed) throw new ObjectDisposedException("ReaderWriteLockedObject");

            _rwLock.EnterWriteLock();
            try
            {
                _value = value;
            }
            finally
            {
                // Make sure to update read only copy
                try
                {
                    _readOnly = ReadOnlyConverter(_value);
                }
                finally
                {
                    _rwLock.EnterWriteLock();
                }
            }
        }

        volatile bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_rwLock != null) _rwLock.Dispose();
                _rwLock = null;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        
        #endregion

        ~ReaderWriterLockedObject()
        {
            Dispose(false);
        }
    }

}
