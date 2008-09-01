using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Provides support for message transformation. The TransformationDispatcher transforms a PublishRequest and publishes the transformed request to the bus.
    /// </summary>
    [Serializable]
    [DataContract]
    public abstract class TransformationDispatcher : Dispatcher
    {
        protected TransformationDispatcher()
        {
            
        }

        /// <summary>
        /// Performs a transformation of a PublishRequest
        /// </summary>
        /// <param name="request">An incoming publish request.</param>
        /// <returns>The transformed request or null if the message cannot be transformed.</returns>
        protected abstract PublishRequest Transform(PublishRequest request);

        /// <summary>
        /// The name of the context key that stores a the IDs (Guid[]) of the endpoints which have been involved in message transformation.
        /// </summary>
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
            if (!oldTransformedByList.Contains(endpoint.Id) || AllowMultipleTransforms)
            {
                Guid[] newTransformedByList = new Guid[oldTransformedByList.Length + 1];
                newTransformedByList[newTransformedByList.Length - 1] = endpoint.Id;
                newContext[TransformedByKeyName] = newTransformedByList;

                context = newContext.MakeReadOnly();

                PublishRequest result = Transform(new PublishRequest(endpoint.ContractType, action, message, context));
                if (result != null)
                {
                    Runtime.Publish(new PublishRequest(result.ContractType, result.Action, result.Message, context));
                }
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation("Skipping already transformed message (" + DispatchContext.MessageDelivery.MessageId +")"); 
            }
        }

        /// <summary>
        /// Gets or sets whether this endpoint can transform a message multiple times.
        /// </summary>
        /// <remarks>
        /// Setting this property to true can cause cycles if it transforms both to and from a certain message type.
        /// </remarks>
        [DataMember]
        public bool AllowMultipleTransforms
        {
            get;
            set;
        }
    }
}
