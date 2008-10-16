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
        public MessageContractMessageDeliveryConverter(Type contractType) : this(ContractDescription.GetContract(contractType))
        {
            
        }

        public MessageContractMessageDeliveryConverter(ContractDescription contract)
        {
            foreach (OperationDescription operation in contract.Operations)
            {
                foreach (MessageDescription message in operation.Messages)
                {
                    if (message.MessageType == null && message.Body.Parts.Count > 0)
                    {
                        throw new InvalidOperationException("Unsupported message contract format");
                    }
                    cacheConverter(message.Action, message.MessageType);
                }
            }
        }

        Dictionary<string, TypedMessageConverter> _converterHash = new Dictionary<string, TypedMessageConverter>();

        void cacheConverter(string action, Type messageType)
        {
            if (!_converterHash.ContainsKey(action))
            {                
                _converterHash.Add(action, messageType == null ? null : (TypedMessageConverter)TypedMessageConverter.Create(messageType, action));
            }
        }

        TypedMessageConverter getCachedConverter(string action)
        {
            try
            {
                return (TypedMessageConverter)_converterHash[action];
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException("Unsupported action");
            }
        }

        protected override object GetMessageObject(Message message)
        {            
            TypedMessageConverter converter = getCachedConverter(message.Headers.Action);
            return converter.FromMessage(message);
        }

        protected override Message ToMessageCore(MessageDelivery delivery)
        {
            if (!_converterHash.ContainsKey(delivery.Action))
            {
                throw new InvalidOperationException("Unsupported action");
            }

            TypedMessageConverter converter = getCachedConverter(delivery.Action);
            if (converter != null)
            {
                return converter.ToMessage(delivery.Message);
            }
            else
            {
                return null;
            }
        }

        public override IEnumerable<string> SupportedActions
        {
            get 
            {
                return _converterHash.Keys;
            }
        }
    }

    
}
