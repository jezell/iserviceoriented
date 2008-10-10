using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;
using System.Collections.ObjectModel;
using System.ServiceModel.Channels;
using System.Xml;
using System.ServiceModel.Description;
using System.ServiceModel;


namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    internal static class MessageDeliveryConverter
    {
        const string MESSAGE_TYPE_HEADER = "messageType";
        const string MAX_RETRIES_HEADER = "maxRetries";
        const string RETRY_COUNT_HEADER = "retryCount";
        const string MESSAGE_ID_HEADER = "messageId";
        const string SUBSCRIPTION_ID_HEADER = "subscriptionId";
        const string TIME_TO_PROCESS_HEADER = "timeToProcess";
        const string CONTRACT_TYPE_NAME_HEADER = "contractTypeName";

        const string MESSAGING_NAMESPACE = "http://iserviceoriented/servicebus/messaging";

        const string CONTEXT_HEADER = "context";
        
        static bool usesMessageContract(Type messageType)
        {
            return messageType.GetCustomAttributes(true).OfType<MessageContractAttribute>().Count() > 0;
        }

        public static Message ToMessage(MessageDelivery delivery)
        {
            Type objType = delivery.Message.GetType();

            Message msg;
            if (usesMessageContract(objType))
            {
                TypedMessageConverter converter = TypedMessageConverter.Create(objType, delivery.Action);
                msg = converter.ToMessage(delivery.Message);
            }
            else
            {
                msg = System.ServiceModel.Channels.Message.CreateMessage(MessageVersion.Default, delivery.Action, delivery.Message);
            }
            
            var serializer = new DataContractSerializer(objType, MessageDelivery.GetKnownTypes());
            
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(CONTEXT_HEADER, MESSAGING_NAMESPACE, delivery.Context, serializer)); 
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(MESSAGE_TYPE_HEADER, MESSAGING_NAMESPACE, objType.AssemblyQualifiedName));

            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(MAX_RETRIES_HEADER, MESSAGING_NAMESPACE, delivery.MaxRetries));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(RETRY_COUNT_HEADER, MESSAGING_NAMESPACE, delivery.RetryCount));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(MESSAGE_ID_HEADER, MESSAGING_NAMESPACE, delivery.MessageId));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(SUBSCRIPTION_ID_HEADER, MESSAGING_NAMESPACE, delivery.SubscriptionEndpointId));
            if (delivery.TimeToProcess != null) msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(TIME_TO_PROCESS_HEADER, MESSAGING_NAMESPACE, delivery.TimeToProcess));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(CONTRACT_TYPE_NAME_HEADER, MESSAGING_NAMESPACE, delivery.ContractTypeName));

            return msg;
        }

        public static MessageDelivery CreateMessageDelivery(Message msg)
        {

            string messageTypeName = msg.Headers.GetHeader<string>(MESSAGE_TYPE_HEADER, MESSAGING_NAMESPACE);

            Type messageType = Type.GetType(messageTypeName);
            
                
            MessageDeliveryContext context = msg.Headers.GetHeader<MessageDeliveryContext>(CONTEXT_HEADER, MESSAGING_NAMESPACE);
            object value;

            if (usesMessageContract(messageType))
            {
                TypedMessageConverter converter = TypedMessageConverter.Create(Type.GetType(msg.Headers.GetHeader<string>(MESSAGE_TYPE_HEADER, MESSAGING_NAMESPACE)), msg.Headers.Action);
                value = converter.FromMessage(msg);
            }
            else
            {
                var serializer = new DataContractSerializer(messageType, MessageDelivery.GetKnownTypes());

                value = serializer.ReadObject(msg.GetReaderAtBodyContents());
            }

            int maxRetries = msg.Headers.GetHeader<int>(MAX_RETRIES_HEADER, MESSAGING_NAMESPACE);
            int retryCount = msg.Headers.GetHeader<int>(RETRY_COUNT_HEADER, MESSAGING_NAMESPACE);
            string messageId = msg.Headers.GetHeader<string>(MESSAGE_ID_HEADER, MESSAGING_NAMESPACE);
            Guid subscriptionId = msg.Headers.GetHeader<Guid>(SUBSCRIPTION_ID_HEADER, MESSAGING_NAMESPACE);
            DateTime? timeToProcess = null;
            if (msg.Headers.FindHeader(TIME_TO_PROCESS_HEADER, MESSAGING_NAMESPACE) != -1)
            {
                timeToProcess = msg.Headers.GetHeader<DateTime>(TIME_TO_PROCESS_HEADER, MESSAGING_NAMESPACE);
            }
            string contractTypeName = msg.Headers.GetHeader<string>(CONTRACT_TYPE_NAME_HEADER, MESSAGING_NAMESPACE);

            MessageDelivery delivery = new MessageDelivery(messageId, subscriptionId, contractTypeName == null ? null : Type.GetType(contractTypeName), msg.Headers.Action, value, maxRetries, retryCount, timeToProcess, new MessageDeliveryContext(context));
            return delivery;

        }
    }
}
