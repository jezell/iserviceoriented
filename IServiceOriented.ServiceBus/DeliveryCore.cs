using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Globalization;

namespace IServiceOriented.ServiceBus
{
    public abstract class DeliveryCore : RuntimeService
    {
        protected void NotifyUnhandledException(Exception ex, bool isTerminating)
        {
            Runtime.NotifyUnhandledException(ex, isTerminating);
        }

        protected void NotifyDelivery(MessageDelivery delivery)
        {                        
            Runtime.NotifyDelivery(delivery);
        }

        protected void NotifyFailure(MessageDelivery delivery, bool permanent)
        {
            Runtime.NotifyFailure(delivery, permanent);
        }

        public abstract void QueueDelivery(MessageDelivery delivery);
    }       
}
