using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IServiceOriented.ServiceBus.UnitTests
{

    [TestClass]
    public class SqlSubscriptionDbUnitTest
    {
        public SqlSubscriptionDbUnitTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            try
            {
                SqlSubscriptionDB.DropDB(@"(local)\SQLEXPRESS", "ServiceBus");
            }
            catch
            {
            }
            SqlSubscriptionDB.CreateDB(@"(local)\SQLEXPRESS", "ServiceBus");
        }

        [ClassCleanup]        
        public static void Cleanup()
        {
            
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        string _connectionString = @"Data Source=(local)\SQLEXPRESS; Initial Catalog=ServiceBus; Integrated Security=SSPI;";

        [TestMethod]
        public void TestCrud()
        {
            SqlSubscriptionDB db = new SqlSubscriptionDB(_connectionString, new Type[] { typeof(WcfDispatcher<IContract>) }, new Type[] { typeof(WcfListener<IContract>)  }, new Type[] { typeof(PassThroughMessageFilter) });

            Assert.AreEqual(0, db.LoadListenerEndpoints().Count(), "Listener endpoints count should be zero");
            Assert.AreEqual(0, db.LoadSubscriptionEndpoints().Count(), "Subscription endpoints count should be zero");

            ListenerEndpoint listener = new ListenerEndpoint(Guid.NewGuid(), "listener", "ListenerConfig",  "http://localhost/test", typeof(IContract), new WcfListener<IContract>());
            db.CreateListener(listener);

            IEnumerable<ListenerEndpoint> listeners = db.LoadListenerEndpoints();
            Assert.AreEqual(1, listeners.Count(), "Listener endpoints count should be one");

            ListenerEndpoint savedListener = listeners.First();

            Assert.AreEqual(listener.Name, savedListener.Name, "Name does not match");
            Assert.AreEqual(listener.Id, savedListener.Id, "Listener id does not match");
            Assert.AreEqual(listener.ContractType, savedListener.ContractType, "Contract type does not match");
            Assert.AreEqual(listener.ConfigurationName, savedListener.ConfigurationName, "Configuration name does not match");
            Assert.AreEqual(listener.Address, savedListener.Address, "Address does not match");

            SubscriptionEndpoint subscription = new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "SubscriptionConfig", "http://localhost/test/subscription", typeof(IContract), new WcfDispatcher<IContract>(), new PassThroughMessageFilter());            
            db.CreateSubscription(subscription);
            
            IEnumerable<SubscriptionEndpoint> subscriptions = db.LoadSubscriptionEndpoints();
            Assert.AreEqual(1, subscriptions.Count(), "Subscription endpoints count should be one");

            SubscriptionEndpoint savedSubscription = subscriptions.First();

            Assert.AreEqual(subscription.Name, savedSubscription.Name, "Name does not match");
            Assert.AreEqual(subscription.Address, savedSubscription.Address, "Address does not match");
            Assert.AreEqual(subscription.ConfigurationName, savedSubscription.ConfigurationName, "ConfigurationName does not match");
            Assert.AreEqual(subscription.ContractType, savedSubscription.ContractType, "ContractType does not match");
            // TODO: Compare dispatchers
            Assert.AreEqual(subscription.Id, savedSubscription.Id, "Id does not match");
            Assert.AreEqual(subscription.Filter.GetType(), savedSubscription.Filter.GetType(), "Id does not match");
            

            db.DeleteListener(listener.Id);
        }
    }
}
