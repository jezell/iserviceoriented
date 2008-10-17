using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.IO;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    public class ConverterMessageDeliveryReaderFactory : MessageDeliveryReaderFactory
    {
        public ConverterMessageDeliveryReaderFactory(MessageEncoder encoder, params Type[] contracts) : this(encoder, ( from t in contracts select ContractDescription.GetContract(t) ).ToArray())
        {
        }
        
        public ConverterMessageDeliveryReaderFactory(MessageEncoder encoder, params ContractDescription[] contracts)
        {
            Encoder = encoder;
            DefaultMaxSizeOfHeaders = 1024 * 10;
            _contracts = contracts;
        }

        ContractDescription[] _contracts;

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
            return new ConverterMessageDeliveryReader(_contracts, stream, isOwner, Encoder, DefaultMaxSizeOfHeaders);
        }
    }
}
