using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;
using System.Collections.ObjectModel;
namespace IServiceOriented.ServiceBus.Dispatchers
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

        protected TransformationDispatcher(SubscriptionEndpoint endpoint) : base(endpoint)
        {

        }

        /// <summary>
        /// Performs a transformation of a PublishRequest
        /// </summary>
        /// <param name="request">An incoming publish request.</param>
        /// <returns>The transformed request or null if the message cannot be transformed.</returns>
        protected abstract PublishRequest Transform(PublishRequest request);

        /// <summary>
        /// The name of the context key that stores a the IDs (ReadOnlyCollection of Guids) of the endpoints which have been involved in message transformation.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly MessageDeliveryContextKey TransformedByKeyName = new MessageDeliveryContextKey("TransformedBy", MessageDelivery.MessagingNamespace);

        private class TransformationList : ReadOnlyCollection<string>
        {
            public TransformationList() : base (new string[0] )
            {
            }

            public TransformationList(IList<string> list) : base(list)
            {
            }
        }

        public override void Dispatch(MessageDelivery messageDelivery)
        {
            MessageDeliveryContext context = messageDelivery.Context;

            Dictionary<MessageDeliveryContextKey, object> newContext = context.ToDictionary();

            TransformationList oldTransformedByList = new TransformationList();

            if (context.ContainsKey(TransformedByKeyName))
            {
                oldTransformedByList = (TransformationList)context[TransformedByKeyName];
            }

            // Don't transform this message more than once
            if (!oldTransformedByList.Contains(Endpoint.Id.ToString()) || AllowMultipleTransforms)
            {
                if (oldTransformedByList.Count() > 0)
                {
                    List<string> list = new List<string>(oldTransformedByList);
                    list.Add(Endpoint.Id.ToString());
                    newContext[TransformedByKeyName] = new TransformationList(list);
                }
                else
                {
                    List<string> list = new List<string>();
                    list.Add(Endpoint.Id.ToString());
                    newContext[TransformedByKeyName] = new TransformationList();
                }

                context = new MessageDeliveryContext(newContext);

                PublishRequest result = Transform(new PublishRequest(Endpoint.ContractType, messageDelivery.Action, messageDelivery.Message, context));
                if (result != null)
                {
                    Runtime.PublishOneWay(new PublishRequest(result.ContractType, result.Action, result.Message, context));
                }
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation("Skipping already transformed message (" + messageDelivery.MessageDeliveryId +")"); 
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
