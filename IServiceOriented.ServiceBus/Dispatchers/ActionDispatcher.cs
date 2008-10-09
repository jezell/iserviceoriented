using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Dispatchers
{
    /// <summary>
    /// Dispatches to an action delegate.
    /// </summary>
    /// <remarks>
    /// This class is not serializable. Do not attempt to pass it across the wire. Use for local dispatch only.
    /// </remarks>
    public class ActionDispatcher : Dispatcher
    {
        public ActionDispatcher(Action<SubscriptionEndpoint, MessageDelivery> dispatchAction)
        {
            DispatchAction = dispatchAction;
        }

        public Action<SubscriptionEndpoint, MessageDelivery> DispatchAction
        {
            get;
            private set;
        }

        public override void Dispatch(MessageDelivery messageDelivery)
        {
            DispatchAction(Endpoint, messageDelivery);
        }
    }
}
