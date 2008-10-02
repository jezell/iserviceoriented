using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IServiceOriented.ServiceBus.UnitTests
{
    /// <summary>
    /// Summary description for ServiceBusManagementTest
    /// </summary>
    [TestClass]
    public class ServiceBusManagementTest
    {
        public ServiceBusManagementTest()
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

        void putMessageAndGet(object message)
        {
            SubscriptionEndpoint subscription = new SubscriptionEndpoint(Guid.NewGuid(), "test", "configurationName", "address", typeof(IServiceBusManagementService), new WcfDispatcher(), null);
            MessageDelivery failure = new MessageDelivery(subscription.Id, typeof(MessageDelivery), "action", message, 3, new ReadOnlyDictionary<string,object>());
            
            NonTransactionalMemoryQueue failureQueue = new NonTransactionalMemoryQueue();
            failureQueue.Enqueue(failure);
            ServiceBusRuntime runtime = new ServiceBusRuntime(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), failureQueue, SimpleServiceLocator.With(new WcfManagementService()));
            
            runtime.Start();

            Service.Use<IServiceBusManagementService>(managementService =>
            {
                Collection<MessageDelivery> failures = managementService.ListMessagesInFailureQueue(null, null);
                Assert.AreEqual(1, failures.Count);
                MessageDelivery dequeued = failures[0];
                Assert.AreEqual(dequeued.MessageId, failure.MessageId);
                Assert.AreEqual(dequeued.MaxRetries, failure.MaxRetries);
                Assert.IsTrue(dequeued.Message.Equals(failure.Message));
                Assert.AreEqual(dequeued.QueueCount, failure.QueueCount);
                Assert.AreEqual(dequeued.TimeToProcess, failure.TimeToProcess);
                Assert.AreEqual(dequeued.SubscriptionEndpointId, failure.SubscriptionEndpointId);
                Assert.AreEqual(dequeued.Action, failure.Action);
            });

            runtime.Stop();
        }
        
        [TestMethod]
        public void TestCanGetFailureQueueComplexMessages()
        {
            MessageDelivery.ClearKnownTypes();
            MessageDelivery.RegisterKnownType(typeof(ComplexData));
            putMessageAndGet(new ComplexData(1000, 12012));
        }

/*        [TestMethod]
        public void TestCanGetFailureQueueMessages()
        {
            putMessageAndGet("test");
        }*/
    }
}
