using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace IServiceOriented.ServiceBus
{
    public interface ILockedObject<TRead, TWrite> : IDisposable
    {
        void Read(Action<TRead> action);
        void Write(Action<TWrite> action);
        
        void SetValue(TWrite value);
            
    }
    
    
    
         

}
