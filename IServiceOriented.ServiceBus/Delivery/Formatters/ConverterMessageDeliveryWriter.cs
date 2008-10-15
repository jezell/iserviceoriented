using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.IO;
using System.ServiceModel.Channels;
using System.IO;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    internal class ConverterMessageDeliveryWriter<T> : MessageDeliveryWriter
    {
        public ConverterMessageDeliveryWriter(Stream baseStream, bool isOwner, MessageEncoder encoder) : base(baseStream, isOwner)
        {
            Encoder = encoder;
        }

        public MessageEncoder Encoder
        {
            get;
            private set;
        }

        MessageDeliveryConverter _converter;
        public MessageDeliveryConverter Converter
        {
            get
            {
                if (_converter == null)
                {
                    _converter = MessageDeliveryConverter.CreateConverter(typeof(T));
                }
                return _converter;
            }
        }

        public override void Write(MessageDelivery delivery)
        {
            Message message = Converter.ToMessage(delivery);
            Encoder.WriteMessage(message, BaseStream);
            BaseStream.Flush();
        }
    }
}
