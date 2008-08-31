using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
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

        public SubscriptionEndpoint(Guid id, string name, string configurationName, string address, string contractTypeName, Dispatcher dispatcher, MessageFilter filter)
            : base(id, name, configurationName, address, contractTypeName)
        {
            Filter = filter;
            Dispatcher = dispatcher;
        }

        MessageFilter _filter;
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
        [DataMember]
        public Dispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
            private set
            {
                if (value != null && value.Endpoint != null)
                {
                    throw new InvalidOperationException("Endpoint is attached to another dispatcher");
                }
                if (_dispatcher != null)
                {
                    _dispatcher.Endpoint = null;
                }
                _dispatcher = value;
                if (value != null) value.Endpoint = this;
            }
        }
    }	
}
