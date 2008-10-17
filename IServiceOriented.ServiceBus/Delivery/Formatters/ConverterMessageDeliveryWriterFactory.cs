using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.IO;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{
    public class ConverterMessageDeliveryWriterFactory : MessageDeliveryWriterFactory
    {
        public ConverterMessageDeliveryWriterFactory(MessageEncoder encoder, params Type[] contracts) : this(encoder, ( from t in contracts select ContractDescription.GetContract(t) ).ToArray())
        {
        }
        public ConverterMessageDeliveryWriterFactory(MessageEncoder encoder, params ContractDescription[] contracts)
        {
            Encoder = encoder;
            _contracts = contracts;
        }

        ContractDescription[] _contracts;

        public MessageEncoder Encoder
        {
            get;
            private set;
        }

        public override MessageDeliveryWriter CreateWriter(System.IO.Stream stream, bool isOwner)
        {
            return new ConverterMessageDeliveryWriter(_contracts, stream, isOwner, Encoder);
        }
    }
}
