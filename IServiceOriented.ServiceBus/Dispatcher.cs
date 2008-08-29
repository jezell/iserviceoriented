using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public abstract class Dispatcher : IDisposable
    {
        public ServiceBusRuntime Runtime
        {
            get;
            private set;
        }

        public SubscriptionEndpoint Endpoint
        {
            get;
            private set;
        }

        internal void StartInternal(ServiceBusRuntime runtime, SubscriptionEndpoint endpoint)
        {
            try
            {
                Runtime = runtime;
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
                Runtime = null;
                Endpoint = null;

                Started = false;
            }
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnStop()
        {
        }

        public bool Started
        {
            get;
            private set;
        }

        [ThreadStatic]
        static DispatchContext _dispatchContext;
        /// <summary>
        /// Contains information about the message that is currently being dispatched.
        /// </summary>
        public static DispatchContext DispatchContext
        {
            get
            {
                return _dispatchContext;
            }
        }
        

        /// <summary>
        /// Called by ServiceBusRuntime to dispatch a message. Ensures that context is set up and torn down.
        /// </summary>        
        internal void DispatchInternal(MessageDelivery delivery)
        {
            _dispatchContext = new DispatchContext(Runtime, delivery);
            try
            {
                Dispatch(Runtime.GetSubscription(delivery.SubscriptionEndpointId), delivery.Action, delivery.Message);
            }
            finally
            {
                _dispatchContext = null;
            }

        }

        /// <summary>
        /// Handles sending a message to a subscriber endpoint.
        /// </summary>
        protected abstract void Dispatch(SubscriptionEndpoint endpoint, string action, object message);

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


    public class DispatchContext
    {
        public DispatchContext()
        {
        }

        public DispatchContext(ServiceBusRuntime runtime, MessageDelivery messageDelivery)
        {
            MessageDelivery = messageDelivery;
            Runtime = runtime;
        }

        public ServiceBusRuntime Runtime
        {
            get;
            private set;
        }

        public MessageDelivery MessageDelivery
        {
            get;
            private set;
        }
    }	
	
}
