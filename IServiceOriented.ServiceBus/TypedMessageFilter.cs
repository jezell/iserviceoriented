using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// A filter that filters out any messages not in a list of predefined types.
    /// </summary>
    [DataContract]   
    public class TypedMessageFilter : MessageFilter
    {
        public TypedMessageFilter(Type messageType)
        {
            _messageTypes = new Type[] {  messageType };
        }

        public TypedMessageFilter(params Type[] messageTypes)
        {
            _messageTypes = (Type[])messageTypes.Clone();
        }

        public TypedMessageFilter(bool inherit, Type messageType)
        {
            Inherit = inherit;
            _messageTypes = new Type[] { messageType };
        }

        public TypedMessageFilter(bool inherit, params Type[] messageTypes)
        {
            Inherit = inherit;
            _messageTypes = (Type[])messageTypes.Clone();
        }
        
        public override bool Include(PublishRequest request)
        {
            if (request.Message == null) return false;

            if (!Inherit)
            {
                return _messageTypes.Contains(request.Message.GetType());
            }
            else
            {
                Type requestType = request.Message.GetType();
                foreach (Type t in _messageTypes)
                {
                    if (t.IsAssignableFrom(requestType))
                    {
                        return true;
                    }
                }
                return false;
            }            
        }

        const char TYPE_SEPERATOR = ':';
        
        [DataMember]
        protected IEnumerable<string> MessageTypeNames
        {
            get
            {
                return _messageTypes.Select(s => s.AssemblyQualifiedName).ToArray();
            }
            set
            {
                _messageTypes = value.Select(s => Type.GetType(s)).ToArray();
            }
        }

        [DataMember]
        public bool Inherit
        {
            get;
            private set;
        }

        public Type[] GetMessageTypes()
        {         
            return (Type[])_messageTypes.Clone();
        }

        Type[] _messageTypes;
    }
}
