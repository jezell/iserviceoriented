using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Base class used to define a service that can extend the functionality of the service bus
    /// </summary>
    public abstract class RuntimeService : IDisposable
    {
        protected ServiceBusRuntime Runtime
        {
            get;
            private set;
        }

        volatile bool _started;
        /// <summary>
        /// Gets a value indicating whether this service has been started
        /// </summary>
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

        /// <summary>
        /// Called when the service is starting
        /// </summary>
        protected virtual void OnStart()
        {
            
        }

        /// <summary>
        /// Called when the service is stopping
        /// </summary>
        protected virtual void OnStop()
        {
            
        }        

        /// <summary>
        /// Called when a listener is added to the service bus
        /// </summary>
        /// <param name="endpoint"></param>
        protected virtual internal void OnListenerAdded(ListenerEndpoint endpoint)
        {
        }

        /// <summary>
        /// Called when a listener is removed from the service bus
        /// </summary>
        /// <param name="endpoint"></param>
        protected virtual internal void OnListenerRemoved(ListenerEndpoint endpoint)
        {
        }

        /// <summary>
        /// Called when a subscription is added to the service bus
        /// </summary>
        /// <param name="endpoint"></param>
        protected virtual internal void OnSubscriptionAdded(SubscriptionEndpoint endpoint)
        {
        }

        /// <summary>
        /// Called when a service is removed from the service bus
        /// </summary>
        /// <param name="endpoint"></param>
        protected virtual internal void OnSubscriptionRemoved(SubscriptionEndpoint endpoint)
        {
        }

        /// <summary>
        /// Called when a message is successfully delivered by the service bus
        /// </summary>
        /// <param name="delivery"></param>
        protected virtual internal void OnMessageDelivered(MessageDelivery delivery)
        {            
        }

        /// <summary>
        /// Called when a message delivery fails
        /// </summary>
        /// <param name="delivery"></param>
        /// <param name="permanent">Indicates if the message will be retried (false) or placed in the failure queue (true)</param>
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
