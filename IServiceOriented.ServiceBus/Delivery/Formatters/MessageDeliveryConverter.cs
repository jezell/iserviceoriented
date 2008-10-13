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
using System.Collections;
using IServiceOriented.ServiceBus.Listeners;


namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    public abstract class MessageDeliveryConverter
    {
        public const string MessageTypeHeader = "messageType";
        public const string MaxRetriesHeader = "maxRetries";
        public const string RetryCountHeader = "retryCount";
        public const string MessageIdHeader = "messageId";
        public const string SubscriptionIdHeader = "subscriptionId";
        public const string TimeToProcessHeader = "timeToProcess";
        public const string ContractTypeNameHeader = "contractTypeName";
        public const string ContextHeader = "context";
                
        public const string MessagingNamespace = "http://iserviceoriented/servicebus/messaging";
        
        protected abstract Message ToMessageCore(MessageDelivery delivery);
        protected abstract object GetMessageObject(Message message);

        public Message ToMessage(MessageDelivery delivery)
        {
            Type objType = delivery.Message.GetType();

            Message msg = ToMessageCore(delivery);                        
            
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(ContextHeader, MessagingNamespace, delivery.Context)); 
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(MessageTypeHeader, MessagingNamespace, objType.AssemblyQualifiedName));

            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(MaxRetriesHeader, MessagingNamespace, delivery.MaxRetries));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(RetryCountHeader, MessagingNamespace, delivery.RetryCount));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(MessageIdHeader, MessagingNamespace, delivery.MessageId));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(SubscriptionIdHeader, MessagingNamespace, delivery.SubscriptionEndpointId));
            if (delivery.TimeToProcess != null) msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(TimeToProcessHeader, MessagingNamespace, delivery.TimeToProcess));
            msg.Headers.Add(System.ServiceModel.Channels.MessageHeader.CreateHeader(ContractTypeNameHeader, MessagingNamespace, delivery.ContractTypeName));


            return msg;
        }

        public MessageDelivery CreateMessageDelivery(Message msg)
        {
            string messageTypeName = msg.Headers.GetHeader<string>(MessageTypeHeader, MessagingNamespace);
                
            MessageDeliveryContext context = msg.Headers.GetHeader<MessageDeliveryContext>(ContextHeader, MessagingNamespace);
            object value = GetMessageObject(msg);            

            int maxRetries = msg.Headers.GetHeader<int>(MaxRetriesHeader, MessagingNamespace);
            int retryCount = msg.Headers.GetHeader<int>(RetryCountHeader, MessagingNamespace);
            string messageId = msg.Headers.GetHeader<string>(MessageIdHeader, MessagingNamespace);
            Guid subscriptionId = msg.Headers.GetHeader<Guid>(SubscriptionIdHeader, MessagingNamespace);
            DateTime? timeToProcess = null;
            if (msg.Headers.FindHeader(TimeToProcessHeader, MessagingNamespace) != -1)
            {
                timeToProcess = msg.Headers.GetHeader<DateTime>(TimeToProcessHeader, MessagingNamespace);
            }
            string contractTypeName = msg.Headers.GetHeader<string>(ContractTypeNameHeader, MessagingNamespace);

            MessageDelivery delivery = new MessageDelivery(messageId, subscriptionId, contractTypeName == null ? null : Type.GetType(contractTypeName), msg.Headers.Action, value, maxRetries, retryCount, timeToProcess, new MessageDeliveryContext(context));
            return delivery;
        }

        public static MessageDeliveryConverter CreateConverter(Type interfaceType)
        {
            if (WcfUtils.UsesMessageContracts(interfaceType))
            {
                return new MessageContractMessageDeliveryConverter(interfaceType);
            }
            else
            {
                return new DataContractMessageDeliveryConverter(interfaceType);
            }
        }
    }

    public class MessageContractMessageDeliveryConverter : MessageDeliveryConverter
    {
        public MessageContractMessageDeliveryConverter(Type contractType)
        {
            foreach (WcfMessageInformation information in WcfUtils.GetMessageInformation(contractType))
            {
                cacheConverter(information.MessageType, information.Action);
            }
        }
        
        Dictionary<string, TypedMessageConverter> _converterHash = new Dictionary<string,TypedMessageConverter>();

        void cacheConverter(Type objType, string action)
        {
            string key = objType + ":" + action;
            if (!_converterHash.ContainsKey(key))
            {
                _converterHash.Add(key, (TypedMessageConverter)TypedMessageConverter.Create(objType, action));
            }
        }

        TypedMessageConverter getCachedConverter(Type objType, string action)
        {
            string key = objType + ":" + action;
            try
            {
                return (TypedMessageConverter)_converterHash[key];
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException("Unsupported interface or action");
            }
        }
        
        protected override object GetMessageObject(Message message)
        {
            TypedMessageConverter converter = getCachedConverter(Type.GetType(message.Headers.GetHeader<string>(MessageTypeHeader, MessagingNamespace)), message.Headers.Action);
            return converter.FromMessage(message);            
        }

        protected override Message ToMessageCore(MessageDelivery delivery)
        {
            Type objType = delivery.Message.GetType();

            TypedMessageConverter converter = getCachedConverter(objType, delivery.Action);            
            return converter.ToMessage(delivery.Message);
        }
    }

    public class DataContractMessageDeliveryConverter : MessageDeliveryConverter
    {
        public DataContractMessageDeliveryConverter(Type interfaceType)
        {
            foreach (WcfMessageInformation information in WcfUtils.GetMessageInformation(interfaceType))
            {
                cacheSerializer(interfaceType, information.MessageType);
            }
        }

        Dictionary<Type, DataContractSerializer> _serializers = new Dictionary<Type, DataContractSerializer>();

        void cacheSerializer(Type interfaceType, Type messageType)
        {
            if (!_serializers.ContainsKey(messageType))
            {
                _serializers.Add(messageType, new DataContractSerializer(messageType, WcfUtils.GetServiceKnownTypes(interfaceType)));
            }
        }

        DataContractSerializer getSerializer(Type interfaceType)
        {
            try
            {
                return _serializers[interfaceType];
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException("Unsupported interface or action");
            }
        }

        protected override Message ToMessageCore(MessageDelivery delivery)
        {
            return System.ServiceModel.Channels.Message.CreateMessage(MessageVersion.Default, delivery.Action, delivery.Message);
        }

        protected override object GetMessageObject(Message message)
        {
            var serializer = getSerializer(Type.GetType(message.Headers.GetHeader<string>(MessageTypeHeader, MessagingNamespace)));
            return serializer.ReadObject(message.GetReaderAtBodyContents());            
        }
    }
}
