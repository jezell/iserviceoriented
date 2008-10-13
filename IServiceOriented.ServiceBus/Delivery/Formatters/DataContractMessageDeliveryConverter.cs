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
