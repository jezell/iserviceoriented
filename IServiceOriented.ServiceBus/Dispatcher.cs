using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;


namespace IServiceOriented.ServiceBus
{    
    /// <summary>
    /// Base class for all service bus dispatchers.
    /// </summary>
    /// <remarks>
    /// Dispatchers are responsible for delivering messages to subscription endpoints.
    /// </remarks>
    [Serializable]
    [DataContract]
    public abstract class Dispatcher : IDisposable
    {
        [NonSerialized]
        ServiceBusRuntime _runtime;
        /// <summary>
        /// Gets the service bus instance associated with this dispatcher.
        /// </summary>
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

        /// <summary>
        /// Handles sending a message to a subscriber endpoint.
        /// </summary>
        public abstract void Dispatch(SubscriptionEndpoint endpoint, MessageDelivery messageDelivery);

        /// <summary>
        /// Dispose any resources held by this dispatcher.
        /// </summary>
        /// <param name="disposing">Indicates whether this method is being called as a result of a direct call to Dispose (true) or by the object's finalizer (false).</param>
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

        ~Dispatcher()
        {
            Dispose(false);
        }
    }


    public class DispatchContext
    {
        public DispatchContext()
        {
        }

        public DispatchContext(ServiceBusRuntime runtime, SubscriptionEndpoint endpoint, MessageDelivery messageDelivery)
        {
            MessageDelivery = messageDelivery;
            Runtime = runtime;
            Endpoint = endpoint;
        }

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

        public MessageDelivery MessageDelivery
        {
            get;
            private set;
        }
    }	
	
}
