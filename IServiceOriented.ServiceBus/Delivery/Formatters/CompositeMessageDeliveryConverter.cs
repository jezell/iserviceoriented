using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    internal class CompositeMessageDeliveryConverter : MessageDeliveryConverter
    {
        public CompositeMessageDeliveryConverter(params MessageDeliveryConverter[] converters)
        {
            foreach (MessageDeliveryConverter converter in converters)
            {
                foreach (string supportedAction in converter.SupportedActions)
                {
                    if (!_converterMap.ContainsKey(supportedAction))
                    {
                        _converterMap.Add(supportedAction, converter);
                    }
                    else
                    {
                        throw new InvalidOperationException("The action " + supportedAction + " cannot be mapped multiple times");
                    }
                }
            }
        }

        protected override object GetMessageObject(System.ServiceModel.Channels.Message message)
        {
            MessageDeliveryConverter converter;
            if (_converterMap.TryGetValue(message.Headers.Action, out converter))
            {
                return converter.ToMessageDelivery(message);
            }
            else
            {
                throw new InvalidOperationException("Unknown action " + message.Headers.Action);
            }
        }

        protected override System.ServiceModel.Channels.Message ToMessageCore(MessageDelivery delivery)
        {
            MessageDeliveryConverter converter;
            if (_converterMap.TryGetValue(delivery.Action, out converter))
            {
                return converter.ToMessage(delivery);
            }
            else
            {
                throw new InvalidOperationException("Unknown action " + delivery.Action);
            }
        }

        Dictionary<string, MessageDeliveryConverter> _converterMap = new Dictionary<string, MessageDeliveryConverter>();
        public override IEnumerable<string> SupportedActions
        {
            get { return _converterMap.Keys; }
        }
    }
}
