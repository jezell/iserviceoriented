using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Services
{
    /// <summary>
    /// Base class for subscription persistence
    /// </summary>
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
                    Runtime.RemoveSubscription(se);
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

        /// <summary>
        /// Load saved end points from the persistence store.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<Endpoint> LoadEndpoints();

        /// <summary>
        /// Create a subscription in the persistence store.
        /// </summary>
        /// <param name="subscription"></param>
        protected abstract void CreateSubscription(SubscriptionEndpoint subscription);
        
        /// <summary>
        /// Delete a subscription from the persistence store.
        /// </summary>
        /// <param name="subscription"></param>
        protected abstract void DeleteSubscription(SubscriptionEndpoint subscription);

        /// <summary>
        /// Create a listener in the persistence store
        /// </summary>
        /// <param name="endpoint"></param>
        protected abstract void CreateListener(ListenerEndpoint endpoint);
        
        /// <summary>
        /// Delete a listener from the persistence store
        /// </summary>
        /// <param name="endpoint"></param>
        protected abstract void DeleteListener(ListenerEndpoint endpoint);

    }

}
