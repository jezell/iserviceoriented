using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Delivery;

namespace IServiceOriented.ServiceBus.UnitTests
{
    public class NullMessageDeliveryQueue : IMessageDeliveryQueue
    {
        private NullMessageDeliveryQueue()
        {
        }
        #region IMessageDeliveryQueue Members

        public void Enqueue(MessageDelivery value)
        {
         
        }

        public MessageDelivery Peek(TimeSpan timeout)
        {
            return null;
        }

        public MessageDelivery Dequeue(TimeSpan timeout)
        {
            return null;
        }

        public MessageDelivery Dequeue(string id, TimeSpan timeout)
        {
            return null;
        }

        public IEnumerable<MessageDelivery> ListMessages()
        {
            return null;
        }

        #endregion

        public static readonly NullMessageDeliveryQueue Instance = new NullMessageDeliveryQueue();
    }
}
