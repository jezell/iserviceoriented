using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    [DataContract]   
    public class TypedMessageFilter : MessageFilter
    {
        public TypedMessageFilter(Type messageType)
        {
            _messageTypes = new Type[] {  messageType };
        }

        
        public override bool Include(string action, object message)
        {
            if (message == null) return false;
            
            return _messageTypes.Contains(message.GetType());
        }

        const char TYPE_SEPERATOR = ':';
        
        [DataMember]
        protected IEnumerable<string> MessageTypeNames
        {
            get
            {
                return _messageTypes.Select(s => s.AssemblyQualifiedName);
            }
            set
            {
                _messageTypes = value.Select(s => Type.GetType(s)).ToArray();
            }
        }

        public Type[] GetMessageTypes()
        {         
            return (Type[])_messageTypes.Clone();
        }

        Type[] _messageTypes;
    }
}
