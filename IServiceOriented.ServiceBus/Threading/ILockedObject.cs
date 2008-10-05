using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace IServiceOriented.ServiceBus.Threading
{
    /// <summary>
    /// Gaurds an object with locks.
    /// </summary>
    /// <typeparam name="TRead">Read only type of the contained data.</typeparam>
    /// <typeparam name="TWrite">Writeable type of the contained data.</typeparam>
    public interface ILockedObject<TRead, TWrite> : IDisposable
    {
        /// <summary>
        /// Access the contained value for read access.
        /// </summary>
        /// <param name="action"></param>
        void Read(Action<TRead> action);
        /// <summary>
        /// Access the contained value for write access.
        /// </summary>
        /// <param name="action"></param>
        void Write(Action<TWrite> action);
        
        /// <summary>
        /// Access the contained value with write access and set to a new value.
        /// </summary>
        /// <param name="value"></param>
        void SetValue(TWrite value);
            
    }
    
    
    
         

}
