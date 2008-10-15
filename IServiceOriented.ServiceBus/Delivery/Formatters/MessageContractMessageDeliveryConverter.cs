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
    internal class MessageContractMessageDeliveryConverter : MessageDeliveryConverter
    {
        public MessageContractMessageDeliveryConverter(Type contractType)
        {
            foreach (WcfMessageInformation information in WcfUtils.GetMessageInformation(contractType))
            {                
                cacheConverter(information.MessageType, information.Action);
            }
        }

        Dictionary<string, TypedMessageConverter> _converterHash = new Dictionary<string, TypedMessageConverter>();

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

    
}
