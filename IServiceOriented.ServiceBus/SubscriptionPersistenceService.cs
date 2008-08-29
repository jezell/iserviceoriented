using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public abstract class SubscriptionPersistenceService : RuntimeService
    {

        protected override void OnStart()
        {

            foreach (Endpoint e in LoadEndpoints())
            {
                ListenerEndpoint le = e as ListenerEndpoint;
                SubscriptionEndpoint se = e as SubscriptionEndpoint;

                if (le != null)
                {
                    _managedEndpoints.Add(le);
                }
                else if (se != null)
                {
                    _managedEndpoints.Add(se);
                }
                else
                {
                    throw new InvalidOperationException("Invalid endpoint type encountered");
                }
            }
        }

        List<Endpoint> _managedEndpoints = new List<Endpoint>();

        protected override void OnStop()
        {
            // Clear out all the stuff we loaded
            foreach (Endpoint e in _managedEndpoints)
            {
                ListenerEndpoint le = e as ListenerEndpoint;
                SubscriptionEndpoint se = e as SubscriptionEndpoint;

                if (le != null)
                {
                    Runtime.RemoveListener(le);
                }
                else if (se != null)
                {
                    Runtime.Unsubscribe(se);
                }
                else
                {
                    throw new InvalidOperationException("Unexpected endpoint type encountered");
                }
            }
        }

        protected internal override void OnListenerAdded(ListenerEndpoint endpoint)
        {
            base.OnListenerAdded(endpoint);

            CreateListener(endpoint);
        }

        protected internal override void OnListenerRemoved(ListenerEndpoint endpoint)
        {
            base.OnListenerRemoved(endpoint);

            DeleteListener(endpoint);
        }

        protected internal override void OnSubscriptionAdded(SubscriptionEndpoint endpoint)
        {
            base.OnSubscriptionAdded(endpoint);

            CreateSubscription(endpoint);
        }

        protected internal override void OnSubscriptionRemoved(SubscriptionEndpoint endpoint)
        {
            base.OnSubscriptionRemoved(endpoint);

            DeleteSubscription(endpoint);
        }

        protected abstract IEnumerable<Endpoint> LoadEndpoints();

        protected abstract void CreateSubscription(SubscriptionEndpoint subscription);
        protected abstract void DeleteSubscription(SubscriptionEndpoint subscription);

        protected abstract void CreateListener(ListenerEndpoint endpoint);
        protected abstract void DeleteListener(ListenerEndpoint endpoint);

    }

}
