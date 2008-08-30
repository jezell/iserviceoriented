using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;


namespace IServiceOriented.ServiceBus
{    
    [Serializable]
    [DataContract]
    public abstract class Dispatcher : IDisposable
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
        SubscriptionEndpoint _endpoint;        
        public SubscriptionEndpoint Endpoint
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

        public bool Started
        {
            get;
            private set;
        }

        [ThreadStatic, NonSerialized]
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
