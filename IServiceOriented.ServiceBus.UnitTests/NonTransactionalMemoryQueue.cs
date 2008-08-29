using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.UnitTests
{
    class NonTransactionalMemoryQueue : IMessageDeliveryQueue
    {
        Queue<MessageDelivery> _queue = new Queue<MessageDelivery>();

        public void Enqueue(MessageDelivery value)
        {
            lock (_queue)
            {
                _queue.Enqueue(value);
            }
        }

        public MessageDelivery Peek(TimeSpan timeout)
        {
            lock (_queue)
            {
                try
                {
                    return _queue.Peek();
                }
                catch
                {
                    return null;
                }
            }
        }

        public MessageDelivery Dequeue(TimeSpan timeout)
        {
            lock (_queue)
            {
                try
                {
                    return _queue.Dequeue();
                }
                catch
                {
                    return null;
                }
            }
        }

        public MessageDelivery Dequeue(string id, TimeSpan timeout)
        {
            lock (_queue)
            {
                foreach (MessageDelivery m in _queue)
                {
                    if (m.MessageId == id)
                    {
                        _queue = new Queue<MessageDelivery>( _queue.Except( new MessageDelivery[] { m }));
                        return m;
                    }
                }
            }
            return null;
        }

        public IEnumerable<MessageDelivery> ListMessages()
        {
            lock (_queue)
            {                
                return _queue.ToArray();                
            }
        }

        
    }
}
