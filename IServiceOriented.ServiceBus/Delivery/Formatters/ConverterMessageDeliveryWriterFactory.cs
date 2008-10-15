using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.IO;
using System.ServiceModel.Channels;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    public class ConverterMessageDeliveryWriterFactory<T> : MessageDeliveryWriterFactory
    {
        public ConverterMessageDeliveryWriterFactory(MessageEncoder encoder)
        {
            Encoder = encoder;
        }

        public MessageEncoder Encoder
        {
            get;
            private set;
        }

        public override MessageDeliveryWriter CreateWriter(System.IO.Stream stream, bool isOwner)
        {
            return new ConverterMessageDeliveryWriter<T>(stream, isOwner, Encoder);
        }
    }
}
