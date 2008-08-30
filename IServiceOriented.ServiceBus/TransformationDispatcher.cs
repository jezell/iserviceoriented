using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
namespace IServiceOriented.ServiceBus
{
    [Serializable]
    [DataContract]
    public abstract class TransformationDispatcher : Dispatcher
    {
        protected TransformationDispatcher()
        {
            
        }

        protected abstract PublishRequest Transform(PublishRequest information);

        
        protected override sealed void Dispatch(SubscriptionEndpoint endpoint, string action, object message)
        {
            PublishRequest result = Transform(new PublishRequest(endpoint.ContractType, action, message));
            Runtime.Publish(new PublishRequest(result.Contract, result.Action, result.Message));
        }
    }
}
