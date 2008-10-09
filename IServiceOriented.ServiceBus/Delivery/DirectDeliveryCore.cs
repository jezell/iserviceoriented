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
        public override void Deliver(MessageDelivery delivery)
        {
            SubscriptionEndpoint endpoint = Runtime.GetSubscription(delivery.SubscriptionEndpointId);
            if (endpoint != null) // subscription may be removed
            {
                endpoint.Dispatcher.Dispatch(delivery);
            }
        }
    }
}
