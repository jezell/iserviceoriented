using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.IO;
using System.ServiceModel.Channels;
using System.IO;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    internal class ConverterMessageDeliveryReader : MessageDeliveryReader
    {
        public ConverterMessageDeliveryReader(ContractDescription[] contracts, Stream stream, bool isOwner, MessageEncoder encoder, int maxSizeOfHeaders) : base(stream, isOwner)
        {
            MaxSizeOfHeaders = maxSizeOfHeaders;
            Encoder = encoder;
            _contracts = contracts;
        }

        ContractDescription[] _contracts;

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
                    _converter = MessageDeliveryConverter.CreateConverter(_contracts);
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
