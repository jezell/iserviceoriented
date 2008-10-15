using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Delivery
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IMessageDeliveryQueue 
    {
        void Enqueue(MessageDelivery value);
        MessageDelivery Peek(TimeSpan timeout);
        MessageDelivery Dequeue(TimeSpan timeout);
        MessageDelivery Dequeue(string id, TimeSpan timeout);

        IEnumerable<MessageDelivery> ListMessages();

        bool IsTransactional
        {
            get;
        }
    }
}
