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

        public TypedMessageFilter(params Type[] messageTypes)
        {
            _messageTypes = (Type[])messageTypes.Clone();
        }
        
        public override bool Include(PublishRequest request)
        {
            if (request.Message == null) return false;
            
            return _messageTypes.Contains(request.Message.GetType());
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
