using System;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IServiceOriented.ServiceBus.UnitTests
{
    /// <summary>
    /// Summary description for WcfDispatcher
    /// </summary>
    [TestClass]
    public class WcfDispatcher
    {
        public WcfDispatcher()
        {
            //
            // TODO: Add constructor logic here
            //
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

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestWcfDispatcher()
        {
            /*
            ContractImplementation ci = new ContractImplementation();
            ServiceHost host = new ServiceHost(ci);
            host.Open();

            ServiceBusRuntime runtime = new ServiceBusRuntime(NullMessageDeliveryQueue.Instance, NullMessageDeliveryQueue.Instance, NullMessageDeliveryQueue.Instance);
            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "NamedPipeClient", "net.tcp://localhost/remotehello", typeof(IContract), typeof(WcfDispatcher<IContract>), null);
            runtime.Subscribe(endpoint);

            string message = "blah blah test test";

            WcfDispatcher<IContract> contractDispatcher = new WcfDispatcher<IContract>();
            contractDispatcher.DispatchInternal(runtime, new MessageDelivery(endpoint.Id, "PublishThis", message));

            Assert.AreEqual(1, ci.PublishedMessages.Count, "One message should be published");
            Assert.AreEqual(message, ci.PublishedMessages.Dequeue(), "Messages should match");

            host.Close();*/
        }
    }
}
