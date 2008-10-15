using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.IO;
using System.ServiceModel.Channels;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    public class ConverterMessageDeliveryReaderFactory<T> : MessageDeliveryReaderFactory
    {
        public ConverterMessageDeliveryReaderFactory(MessageEncoder encoder)
        {
            Encoder = encoder;
            DefaultMaxSizeOfHeaders = 1024 * 10;
        }

        public int DefaultMaxSizeOfHeaders
        {
            get;
            set;
        }

        public MessageEncoder Encoder
        {
            get;
            private set;
        }

        public override MessageDeliveryReader CreateReader(System.IO.Stream stream, bool isOwner)
        {
            return new ConverterMessageDeliveryReader<T>(stream, isOwner, Encoder, DefaultMaxSizeOfHeaders);
        }
    }
}
