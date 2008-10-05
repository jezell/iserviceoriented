using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;

using System.IO;

using IServiceOriented.ServiceBus.Data;

namespace IServiceOriented.ServiceBus
{    
    /// <summary>
    /// Provides support for durable persistence of subscriptions and listeners using SQL Server
    /// </summary>
    public class SqlSubscriptionPersistenceService : SubscriptionPersistenceService
    {
        public SqlSubscriptionPersistenceService(ISubscriptionDB db)
        {
            _db = db;
        }

        ISubscriptionDB _db;        
        
        protected override IEnumerable<Endpoint> LoadEndpoints()
        {
            List<Endpoint> endpoints = new List<Endpoint>();
            endpoints.AddRange(_db.LoadListenerEndpoints().OfType<Endpoint>());
            endpoints.AddRange(_db.LoadSubscriptionEndpoints().OfType<Endpoint>());
            return endpoints;
        }


        protected override void CreateListener(ListenerEndpoint endpoint)
        {
            if (!endpoint.Transient)
            {
                _db.CreateListener(endpoint);
            }
        }

        protected override void CreateSubscription(SubscriptionEndpoint subscription)
        {
            if (!subscription.Transient)
            {
                _db.CreateSubscription(subscription);
            }
        }

        protected override void DeleteListener(ListenerEndpoint endpoint)
        {
            if (!endpoint.Transient)
            {
                _db.DeleteListener(endpoint.Id);
            }
        }

        protected override void DeleteSubscription(SubscriptionEndpoint subscription)
        {
            if (!subscription.Transient)
            {
                _db.DeleteSubscription(subscription.Id);
            }
        }
    }
}
