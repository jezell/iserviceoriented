using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

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


        internal void Validate()
        {
            WithLockedState(RuntimeServiceState.Stopped, () =>
            {                
                _state = RuntimeServiceState.Validating;
                try
                {
                    OnValidate();
                }
                catch(Exception ex)
                {
                    _state = RuntimeServiceState.Stopped;
                    throw new ValidationException("The service could not be validated", ex);
                }
                _state = RuntimeServiceState.Validated;
            });
        }

        protected virtual void OnValidate()
        {
        }

        volatile RuntimeServiceState _state = RuntimeServiceState.Stopped;
        public RuntimeServiceState State
        {
            get
            {
                return _state;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this service has been started
        /// </summary>
        protected bool Started
        {
            get
            {
                return (int)_state >= (int)RuntimeServiceState.Started;
            }
        }

        internal void StartInternal(ServiceBusRuntime runtime)
        {
            WithLockedState(RuntimeServiceState.Validated, () =>
            {
                _state = RuntimeServiceState.Starting;

                Runtime = runtime;

                try
                {
                    if (!Started)
                    {
                        OnStart();
                        _state = RuntimeServiceState.Started;
                    }
                }
                finally
                {
                    if (!Started)
                    {
                        Runtime = null;
                        _state = RuntimeServiceState.Error;
                    }
                }
            });
        }
        internal void StopInternal()
        {
            WithLockedState(RuntimeServiceState.Started, () =>
            {
                try
                {
                    _state = RuntimeServiceState.Stopping;
                    OnStop();
                    _state = RuntimeServiceState.Stopped;
                }
                finally
                {
                    if (_state != RuntimeServiceState.Stopped) _state = RuntimeServiceState.Error;
                    Runtime = null;
                }
            });
        }
        
        protected void WithLockedState(Action action )
        {
            lock (_stateTransitionLock)
            {
                action();
            }
        }
      
        protected void WithLockedState(RuntimeServiceState state, Action action)
        {
            lock (_stateTransitionLock)
            {
                if (_state != state)
                {
                    throw new InvalidOperationException("The service must be " + state);
                }
                action();
            }
        }
      

        object _stateTransitionLock = new object();

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
        /// Called when a message delivery expires
        /// </summary>
        /// <param name="delivery"></param>
        protected virtual internal void OnMessageDeliveryExpired(MessageDelivery delivery)
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

    public enum RuntimeServiceState
    {
        None, Stopped, Validating, Validated, Starting, Started, Stopping, Error = -1
    }
}
