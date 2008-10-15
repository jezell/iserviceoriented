using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IServiceOriented.ServiceBus.IO
{
    public abstract class MessageDeliveryReaderFactory
    {
        public MessageDeliveryReader CreateReader(Stream stream)
        {
            return CreateReader(stream, true);
        }
        public abstract MessageDeliveryReader CreateReader(Stream stream, bool isOwner);
    }
}
