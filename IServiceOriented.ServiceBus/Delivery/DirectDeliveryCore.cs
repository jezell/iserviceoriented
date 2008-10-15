using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Delivery
{
    /// <summary>
    /// Dispatches messages directly to subscribers
    /// </summary>
    /// <remarks>
    /// This delivery core can be useful if you do not require queued delivery or are dispatching to endpoints that manage their own queues.
    /// </remarks>
    public class DirectDeliveryCore : DeliveryCore
    {
        public DirectDeliveryCore()
        {
        }
        public DirectDeliveryCore(bool transactional)
        {
            _transactional = transactional;
        }
        public override void Deliver(MessageDelivery delivery)
        {
            if (!Started)
            {
                throw new InvalidOperationException("Cannot deliver messages before the bus is started");
            }
            try
            {
                SubscriptionEndpoint endpoint = Runtime.GetSubscription(delivery.SubscriptionEndpointId);
                if (endpoint != null) // subscription may be removed
                {
                    endpoint.Dispatcher.Dispatch(delivery);
                }
                NotifyDelivery(delivery);
            }
            catch(Exception ex)
            {
                throw new DeliveryException("Unhandled exception while attempting to deliver the message", ex);
            }
        }

        bool _transactional;
        public override bool IsTransactional
        {
            get 
            {
                return _transactional;
            }
        }
    }
}
