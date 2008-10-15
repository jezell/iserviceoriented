using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.IO;
using System.ServiceModel.Channels;
using System.IO;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    public class ConverterMessageDeliveryReader<T> : MessageDeliveryReader
    {
        public ConverterMessageDeliveryReader(Stream stream, bool isOwner, MessageEncoder encoder, int maxSizeOfHeaders) : base(stream, isOwner)
        {
            MaxSizeOfHeaders = maxSizeOfHeaders;
            Encoder = encoder;
        }

        public int MaxSizeOfHeaders
        {
            get;
            private set;
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

        public override MessageDelivery Read()
        {
            Message message = Encoder.ReadMessage(BaseStream, MaxSizeOfHeaders);
            return Converter.ToMessageDelivery(message);
        }    
    }
}
