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

        public PublishRequest(Type contract, string action, object message)
        {
            Contract = contract;
            Action = action;
            Message = message;
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
    }
}
