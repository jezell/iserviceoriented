using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{    
    /// <summary>
    /// Base class for all service host listeners.
    /// </summary>
    /// <remarks>
    /// Listeners are responsible for receiving messages and publishing them to the bus.
    /// </remarks>
    [Serializable]
    [DataContract]
    public abstract class Listener : IDisposable
    {
        [NonSerialized]
        ServiceBusRuntime _runtime;
        /// <summary>
        /// Gets the service bus instance that this listener is associated with.
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

        [NonSerialized]
        ListenerEndpoint _endpoint;
        /// <summary>
        /// Gets the listener endpoint that this listener is associated with.
        /// </summary>
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
        /// <summary>
        /// Gets a boolean value indicating whether this listener has been started.
        /// </summary>
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

        /// <summary>
        /// Perform any actions that should be performed when this listener starts.
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// Perform any actions that should be performed when this listener stops.
        /// </summary>
        protected virtual void OnStop()
        {
        }

        /// <summary>
        /// Dispose any resources held by this listener.
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

        ~Listener()
        {
            Dispose(false);
        }
    }

}    
