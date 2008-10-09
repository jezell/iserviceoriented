using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Represents a subscription endpoint.
    /// </summary>
    [Serializable]
    [DataContract]
    public sealed class SubscriptionEndpoint : Endpoint
    {
        public SubscriptionEndpoint(Guid id, string name, string configurationName, string address, Type contractType, Dispatcher dispatcher, MessageFilter filter)
            : base(id, name, configurationName, address, contractType)
        {
            Filter = filter;
            Dispatcher = dispatcher;
        }


        public SubscriptionEndpoint(Guid id, string name, string configurationName, string address, Type contractType, Dispatcher dispatcher, MessageFilter filter, bool transient)
            : base(id, name, configurationName, address, contractType,  transient)
        {
            Filter = filter;
            Dispatcher = dispatcher;
        }

        public SubscriptionEndpoint(Guid id, string name, string configurationName, string address, string contractTypeName, Dispatcher dispatcher, MessageFilter filter)
            : base(id, name, configurationName, address, contractTypeName)
        {
            Filter = filter;
            Dispatcher = dispatcher;
        }

        MessageFilter _filter;
        /// <summary>
        /// The filter to be applied to this subscription or null if all messages should be included.
        /// </summary>
        [DataMember]
        public MessageFilter Filter
        {
            get
            {
                return _filter;
            }
            private set
            {
                _filter = value;
            }
        }

        Dispatcher _dispatcher;
        /// <summary>
        /// The Dispatcher used to send messages to this endpoint.
        /// </summary>
        [DataMember]
        public Dispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
            private set
            {                
                _dispatcher = value;                
            }
        }
    }	
}
