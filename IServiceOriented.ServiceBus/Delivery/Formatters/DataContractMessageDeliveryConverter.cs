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
    internal class DataContractMessageDeliveryConverter : MessageDeliveryConverter
    {
        public DataContractMessageDeliveryConverter(Type contractType)
            : this(ContractDescription.GetContract(contractType))
        {
            
        }

        public DataContractMessageDeliveryConverter(ContractDescription contract)
        {
            foreach (OperationDescription operation in contract.Operations)
            {
                foreach (MessageDescription message in operation.Messages)
                {
                    cacheSerializer(operation, message);
                }
            }
        }

        Dictionary<string, DataContractSerializer> _serializers = new Dictionary<string, DataContractSerializer>();

        void cacheSerializer(OperationDescription operation, MessageDescription message)
        {
            if (!_serializers.ContainsKey(message.Action))
            {
                if (message.Body.ReturnValue != null)
                {
                    // this is a return message
                    _serializers.Add(message.Action,  new DataContractSerializer(message.Body.ReturnValue.Type, operation.KnownTypes));
                }
                else
                {
                    _serializers.Add(message.Action, message.Body.Parts.Count == 0 ? null : new DataContractSerializer(message.Body.Parts[0].Type, operation.KnownTypes));
                }
            }
        }        
        DataContractSerializer getSerializer(string action)
        {
            try
            {
                return _serializers[action];
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException("Unsupported action");
            }
        }

        protected override Message ToMessageCore(MessageDelivery delivery)
        {
            if (!_serializers.ContainsKey(delivery.Action))
            {
                throw new InvalidOperationException("Unsupported action");
            }

            var serializer = _serializers[delivery.Action];
            // todo: should we default messageversion here?
            if (serializer != null)
            {
                return System.ServiceModel.Channels.Message.CreateMessage(MessageVersion.Default, delivery.Action, delivery.Message, serializer);
            }
            else
            {
                return System.ServiceModel.Channels.Message.CreateMessage(MessageVersion.Default, delivery.Action, delivery.Message);
            }
        }

        protected override object GetMessageObject(Message message)
        {
            var serializer = getSerializer(message.Headers.Action);
            if (serializer != null)
            {
                return serializer.ReadObject(message.GetReaderAtBodyContents());
            }
            else
            {
                return null;
            }
        }

        public override IEnumerable<string> SupportedActions
        {
            get { return _serializers.Keys; }
        }
    }
}
