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

        public const string TransformedByKeyName = "TransformedBy";
        
        protected override sealed void Dispatch(SubscriptionEndpoint endpoint, string action, object message)
        {
            ReadOnlyDictionary<string, object> context = DispatchContext.MessageDelivery.Context;

            Dictionary<string, object> newContext = context.ToDictionary();

            Guid[] oldTransformedByList = new Guid[0];

            if (context.ContainsKey(TransformedByKeyName))
            {
                oldTransformedByList = (Guid[])context[TransformedByKeyName];
            }

            // Don't transform this message more than once
            if (!oldTransformedByList.Contains(endpoint.Id))
            {
                Guid[] newTransformedByList = new Guid[oldTransformedByList.Length + 1];
                newTransformedByList[newTransformedByList.Length - 1] = endpoint.Id;
                newContext[TransformedByKeyName] = newTransformedByList;

                context = newContext.MakeReadOnly();

                PublishRequest result = Transform(new PublishRequest(endpoint.ContractType, action, message, context));
                Runtime.Publish(new PublishRequest(result.Contract, result.Action, result.Message, context));
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation("Skipping already transformed message (" + DispatchContext.MessageDelivery.MessageId +")"); 
            }
        }
    }
}
