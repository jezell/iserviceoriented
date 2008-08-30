using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{    
    [Serializable]
    [DataContract]
    public abstract class Listener : IDisposable
    {
        [NonSerialized]
        ServiceBusRuntime _runtime;
        public ServiceBusRuntime Runtime
        {
            get
            {
                return _runtime;
            }
            internal set
            {                
                _runtime = value;
            }
        }

        [NonSerialized]
        ListenerEndpoint _endpoint;
        public ListenerEndpoint Endpoint
        {
            get
            {
                return _endpoint;
            }
            internal set
            {
                _endpoint = value;
            }
        }

        [NonSerialized]
        bool _started;
        public bool Started
        {
            get
            {
                return _started;
            }
            private set
            {
                _started = value;
            }
        }

        internal void StartInternal()
        {            
            OnStart();
            Started = true;        
        }

        internal void StopInternal()
        {
            OnStop();
            Started = false;
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnStop()
        {
        }

        protected virtual void Dispose(bool disposing)
        {

        }
        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion
    }

}    
