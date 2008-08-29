using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public class RuntimeService : IDisposable
    {
        protected ServiceBusRuntime Runtime
        {
            get;
            private set;
        }

        volatile bool _started;
        protected bool Started
        {
            get
            {
                return _started;
            }
        }
        internal void StartInternal(ServiceBusRuntime runtime)
        {
            Runtime = runtime;

            try
            {
                if (!_started)
                {
                    OnStart();
                    _started = true;
                }
            }
            finally
            {
                if (!_started)
                {
                    Runtime = null;
                }
            }
        }
        internal void StopInternal()
        {
            try
            {
                if (_started)
                {
                    _started = false;
                    OnStop();
                }
            }
            finally
            {
                Runtime = null;
            }
        }

        protected virtual void OnStart()
        {
            
        }

        protected virtual void OnStop()
        {
            
        }        

        protected virtual internal void OnListenerAdded(ListenerEndpoint endpoint)
        {
        }

        protected virtual internal void OnListenerRemoved(ListenerEndpoint endpoint)
        {
        }

        protected virtual internal void OnSubscriptionAdded(SubscriptionEndpoint endpoint)
        {
        }

        protected virtual internal void OnSubscriptionRemoved(SubscriptionEndpoint endpoint)
        {
        }

        protected virtual internal void OnMessageDelivered(MessageDelivery delivery)
        {            
        }

        protected virtual internal void OnMessageDeliveryFailed(MessageDelivery delivery, bool permanent)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
    }
}
