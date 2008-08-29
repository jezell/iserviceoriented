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

        protected internal override string CreateInitString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _messageTypes.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(TYPE_SEPERATOR);
                }
                sb.Append(_messageTypes[i]);
            }
            return sb.ToString();
        }

        protected internal override void InitFromString(string data)
        {
            string[] typeNames = data.Split(TYPE_SEPERATOR);
            Type[] types = new Type[typeNames.Length];
            for (int i = 0; i < typeNames.Length; i++)
            {
                Type t = Type.GetType(typeNames[i]);
                if(t == null) throw new InvalidOperationException("Unknown type");
                types[i] = t;
            }
            _messageTypes = types;            
        }

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
