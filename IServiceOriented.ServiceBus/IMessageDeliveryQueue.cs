using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public interface IMessageDeliveryQueue 
    {
        void Enqueue(MessageDelivery value);
        MessageDelivery Peek(TimeSpan timeout);
        MessageDelivery Dequeue(TimeSpan timeout);
        MessageDelivery Dequeue(string id, TimeSpan timeout);

        IEnumerable<MessageDelivery> ListMessages();
    }
}
