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
        public SubscriptionEndpoint(Guid id, string name, string configurationName, string address, Type contractType, Type dispatcherType, MessageFilter filter) 
            : base(id,  name, configurationName, address, contractType)
        {
            Filter = filter;
            DispatcherType = dispatcherType;
        }

        public SubscriptionEndpoint(Guid id, string name, string configurationName, string address, string contractTypeName, string dispatcherTypeName, MessageFilter filter)
            : base(id, name, configurationName, address, contractTypeName)
        {
            Filter = filter;
            DispatcherTypeName = dispatcherTypeName;
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
        
        Type _dispatcherType;        
        public Type DispatcherType
        {
            get
            {
                return _dispatcherType;
            }
            private set
            {
                _dispatcherType = value;
            }
        }

        [DataMember]
        public string DispatcherTypeName
        {
            get
            {
                return _dispatcherType.AssemblyQualifiedName;
            }
            private set
            {
                _dispatcherType = Type.GetType(value);
                if (_dispatcherType == null)
                {
                    throw new InvalidOperationException(value + " is an invalid type");
                }
            }
        }
    }	    
	
}
