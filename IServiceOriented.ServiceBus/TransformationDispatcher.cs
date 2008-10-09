using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;
using System.Collections.ObjectModel;
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
        /// The name of the context key that stores a the IDs (ReadOnlyCollection of Guids) of the endpoints which have been involved in message transformation.
        /// </summary>
        public const string TransformedByKeyName = "TransformedBy";

        public class TransformationList : ReadOnlyCollection<string>
        {
            public TransformationList() : base (new string[0] )
            {
            }

            public TransformationList(IList<string> list) : base(list)
            {
            }
        }

        public override void Dispatch(SubscriptionEndpoint endpoint, MessageDelivery messageDelivery)
        {
            MessageDeliveryContext context = messageDelivery.Context;

            Dictionary<string, object> newContext = context.ToDictionary();

            TransformationList oldTransformedByList = new TransformationList();

            if (context.ContainsKey(TransformedByKeyName))
            {
                oldTransformedByList = (TransformationList)context[TransformedByKeyName];
            }

            // Don't transform this message more than once
            if (!oldTransformedByList.Contains(endpoint.Id.ToString()) || AllowMultipleTransforms)
            {
                if (oldTransformedByList.Count() > 0)
                {
                    List<string> list = new List<string>(oldTransformedByList);
                    list.Add(endpoint.Id.ToString());
                    newContext[TransformedByKeyName] = new TransformationList(list);
                }
                else
                {
                    List<string> list = new List<string>();
                    list.Add(endpoint.Id.ToString());
                    newContext[TransformedByKeyName] = new TransformationList();
                }

                context = new MessageDeliveryContext(newContext);

                PublishRequest result = Transform(new PublishRequest(endpoint.ContractType, messageDelivery.Action, messageDelivery.Message, context));
                if (result != null)
                {
                    Runtime.Publish(new PublishRequest(result.ContractType, result.Action, result.Message, context));
                }
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation("Skipping already transformed message (" + messageDelivery.MessageId +")"); 
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
