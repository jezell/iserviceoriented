using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IServiceOriented.ServiceBus.IO
{
    public abstract class MessageDeliveryWriterFactory
    {
        public MessageDeliveryWriter CreateWriter(Stream stream)
        {
            return CreateWriter(stream, true);
        }
        public abstract MessageDeliveryWriter CreateWriter(Stream stream, bool isOwner);
    }
}
