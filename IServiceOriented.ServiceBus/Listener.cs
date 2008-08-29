using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public abstract class Listener : IDisposable
    {
        public ServiceBusRuntime Runtime
        {
            get;
            private set;
        }

        public ListenerEndpoint Endpoint
        {
            get;
            private set;
        }

        public bool Started
        {
            get;
            private set;
        }

        internal void StartInternal(ServiceBusRuntime host, ListenerEndpoint endpoint)
        {
            try
            {
                Runtime = host;
                Endpoint = endpoint;

                OnStart();

                Started = true;
            }
            finally
            {
                if (!Started)
                {
                    Runtime = null;
                    Endpoint = null;
                }                
            }
        }

        internal void StopInternal()
        {
            try
            {
                OnStop();
            }
            finally
            {
                Started = false;

                Runtime = null;
                Endpoint = null;
            }
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
