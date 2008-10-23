using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Delivery;
using System.Transactions;
using System.Collections.ObjectModel;

namespace IServiceOriented.ServiceBus.Dispatchers
{
    public class SubscriptionDispatcher : Dispatcher
    {
        public SubscriptionDispatcher(Func<IEnumerable<SubscriptionEndpoint>> getSubscriptions)
        {
            _getSubscriptions = getSubscriptions;
        }

        Func<IEnumerable<SubscriptionEndpoint>> _getSubscriptions;

        List<MessageDelivery> determineDeliveries(PublishRequest publishRequest)
        {
            List<MessageDelivery> messageDeliveries = new List<MessageDelivery>();
            
            List<SubscriptionEndpoint> unhandledFilters = new List<SubscriptionEndpoint>();

            bool handled = false;
            foreach (SubscriptionEndpoint subscription in _getSubscriptions())
            {
                if (!subscription.IsExpired)
                {
                    bool include;

                    if (subscription.Filter is UnhandledMessageFilter)
                    {
                        include = false;
                        if (subscription.Filter.Include(publishRequest))
                        {
                            unhandledFilters.Add(subscription);
                        }
                    }
                    else
                    {
                        include = subscription.Filter == null || subscription.Filter.Include(publishRequest);
                    }

                    if (include)
                    {
                        MessageDelivery delivery = new MessageDelivery(subscription.Id, publishRequest.ContractType, publishRequest.Action, publishRequest.Message, Runtime.MaxRetries, publishRequest.Context);
                        messageDeliveries.Add(delivery);
                        handled = true;
                    }
                }
            }

            // if unhandled, send to subscribers of unhandled messages
            if (!handled)
            {
                foreach (SubscriptionEndpoint subscription in unhandledFilters)
                {
                    if (!subscription.IsExpired)
                    {
                        MessageDelivery delivery = new MessageDelivery(subscription.Id, publishRequest.ContractType, publishRequest.Action, publishRequest.Message, Runtime.MaxRetries, publishRequest.Context);
                        messageDeliveries.Add(delivery);
                    }
                }
            }

            return messageDeliveries;
        }

        public virtual DeliveryCore SelectDeliveryCore(MessageDelivery delivery)
        {
            DeliveryCore deliveryCore = Runtime.ServiceLocator.GetInstance<DeliveryCore>();
            return deliveryCore;
        }        

        public override void Dispatch(MessageDelivery messageDelivery)
        {
            PublishRequest pr = new PublishRequest(messageDelivery.ContractType, messageDelivery.Action, messageDelivery.Message, messageDelivery.Context);
            using (TransactionScope ts = new TransactionScope())
            {
                foreach (MessageDelivery md in determineDeliveries(pr))
                {
                    DeliveryCore deliveryCore = SelectDeliveryCore(md);
                    deliveryCore.Deliver(md);
                }
                ts.Complete();
            }
        }
    }
}
