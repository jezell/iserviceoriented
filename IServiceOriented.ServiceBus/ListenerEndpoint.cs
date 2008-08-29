using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    [Serializable]
    [DataContract]
    public sealed class ListenerEndpoint : Endpoint
    {
        public ListenerEndpoint(Guid id, string name, string configurationName, string address, Type contractType, Type listenerType) : base(id, name, configurationName, address, contractType)
        {
            ListenerType = listenerType;
        }

        public Type ListenerType
        {
            get;
            set;
        }

        [DataMember]
        public string ListenerTypeName
        {
            get
            {
                if (ListenerType == null) return null;
                return ListenerType.AssemblyQualifiedName;
            }
            set
            {
                if (value == null)
                {
                    ListenerType = null;
                }
                else
                {
                    Type type = Type.GetType(value);
                    if (type == null)
                    {
                        throw new InvalidOperationException("Unknown type");
                    }
                    ListenerType = type;

                }
            }
        }
        
    }
}
