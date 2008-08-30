using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    [DataContract]
    public sealed class ListenerEndpoint : Endpoint
    {
        public ListenerEndpoint(Guid id, string name, string configurationName, string address, Type contractType, Listener listener) : base(id, name, configurationName, address, contractType)
        {
            Listener = listener;
        }

        Listener _listener;
        [DataMember]
        public Listener Listener
        {
            get
            {
                return _listener;
            }
            private set
            {
                if (value != null && value.Endpoint != null)
                {
                    throw new InvalidOperationException("Endpoint is attached to another listener");
                }
                if (_listener != null)
                {
                    _listener.Endpoint = null;
                }
                _listener = value;
                if(value != null) value.Endpoint = this;
            }
        }
    }
}
