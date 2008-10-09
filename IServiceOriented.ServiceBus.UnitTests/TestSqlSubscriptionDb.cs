using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using IServiceOriented.ServiceBus.Data;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestSqlSubscriptionDb
    {
        public TestSqlSubscriptionDb()
        {
        }

        [TestFixtureSetUp]
        public void Initialize()
        {            
            SqlSubscriptionDB.CreateDB(@"(local)", "ServiceBus", true);
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            SqlSubscriptionDB.DropConnectionsToDB("(local)", "ServiceBus");
            SqlSubscriptionDB.DropDB(@"(local)", "ServiceBus");               
        }

        string _connectionString = @"Data Source=(local); Initial Catalog=ServiceBus; Integrated Security=SSPI;";

        [Test]
        public void TestCrud()
        {
            SqlSubscriptionDB db = new SqlSubscriptionDB(_connectionString, new Type[] { typeof(WcfDispatcher) }, new Type[] { typeof(WcfListener)  }, new Type[] { typeof(PassThroughMessageFilter) });

            Assert.AreEqual(0, db.LoadListenerEndpoints().Count());
            Assert.AreEqual(0, db.LoadSubscriptionEndpoints().Count());

            ListenerEndpoint listener = new ListenerEndpoint(Guid.NewGuid(), "listener", "ListenerConfig",  "http://localhost/test", typeof(IContract), new WcfListener());
            db.CreateListener(listener);

            IEnumerable<ListenerEndpoint> listeners = db.LoadListenerEndpoints();
            Assert.AreEqual(1, listeners.Count());

            ListenerEndpoint savedListener = listeners.First();

            Assert.AreEqual(listener.Name, savedListener.Name);
            Assert.AreEqual(listener.Id, savedListener.Id);
            Assert.AreEqual(listener.ContractType, savedListener.ContractType);
            Assert.AreEqual(listener.ConfigurationName, savedListener.ConfigurationName);
            Assert.AreEqual(listener.Address, savedListener.Address);

            SubscriptionEndpoint subscription = new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "SubscriptionConfig", "http://localhost/test/subscription", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());            
            db.CreateSubscription(subscription);
            
            IEnumerable<SubscriptionEndpoint> subscriptions = db.LoadSubscriptionEndpoints();
            Assert.AreEqual(1, subscriptions.Count());

            SubscriptionEndpoint savedSubscription = subscriptions.First();

            Assert.AreEqual(subscription.Name, savedSubscription.Name);
            Assert.AreEqual(subscription.Address, savedSubscription.Address);
            Assert.AreEqual(subscription.ConfigurationName, savedSubscription.ConfigurationName);
            Assert.AreEqual(subscription.ContractType, savedSubscription.ContractType);
            // TODO: Compare dispatchers
            Assert.AreEqual(subscription.Id, savedSubscription.Id);
            Assert.AreEqual(subscription.Filter.GetType(), savedSubscription.Filter.GetType());
            

            db.DeleteListener(listener.Id);
        }
    }
}
