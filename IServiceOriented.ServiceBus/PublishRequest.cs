using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public class PublishRequest
    {
        protected PublishRequest()
        {
        }

        public PublishRequest(Type contract, string action, object message) : 
            this(contract, action, message, new ReadOnlyDictionary<string,object>())
        {
        }
        public PublishRequest(Type contract, string action, object message, ReadOnlyDictionary<string, object> context)
        {
            Contract = contract;
            Action = action;
            Message = message;
            Context = context;
        }

        public Type Contract
        {
            get;
            private set;
        }
        public string Action
        {
            get;
            private set;
        }
        public object Message
        {
            get;
            private set;
        }

        public ReadOnlyDictionary<string, object> Context
        {
            get;
            private set;
        }
    }
}
