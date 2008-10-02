using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestClass]
    public class ServiceBusRuntimeTest
    {
        public ServiceBusRuntimeTest()
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

        const string _testQueuePath = ".\\private$\\esb_test_queue";
        const string _retryQueuePath = ".\\private$\\esb_retry_queue";
        const string _failQueuePath = ".\\private$\\esb_fail_queue";
        
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {            
            
        }

        void recreateQueues()
        {
            // Delete test queues if they already exist
            try
            {
                MsmqMessageDeliveryQueue.Delete(_testQueuePath);
            }
            catch
            {
            }
            try
            {
                MsmqMessageDeliveryQueue.Delete(_retryQueuePath);
            }
            catch
            {
            }
            try
            {
                MsmqMessageDeliveryQueue.Delete(_failQueuePath);
            }
            catch
            {
            }

            // Create test queues
            MsmqMessageDeliveryQueue.Create(_testQueuePath);
            MsmqMessageDeliveryQueue.Create(_retryQueuePath);
            MsmqMessageDeliveryQueue.Create(_failQueuePath);  
        }

        [TestMethod]
        public void TestFilterExcludedMessageDispatch()
        {
            recreateQueues();

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);

            ServiceBusRuntime serviceBusRuntime = new ServiceBusRuntime(testQueue, retryQueue, failQueue);

            serviceBusRuntime.ExponentialBackOff = false;
            serviceBusRuntime.RetryDelay = 1000;
            serviceBusRuntime.MaxRetries = 1;


            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/test", typeof(IContract), new WcfListener()));
            string message = "Publish this message";
            ContractImplementation ci = new ContractImplementation();
            ci.SetFailCount(1);

            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), new BooleanMessageFilter(false)));


            AutoResetEvent wait = new AutoResetEvent(false);
            serviceBusRuntime.MessageDelivered += (o, mdea) => { wait.Set(); };
            serviceBusRuntime.MessageDeliveryFailed += (o, mdfea) => { wait.Set(); };

            serviceBusRuntime.Start();

            serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));

            try
            {
                // Wait for delivery
                wait.WaitOne(TimeSpan.FromMinutes(.25), false); // give it a few seconds
            }
            catch
            {
            }

            serviceBusRuntime.Stop();

            Assert.AreEqual(0, ci.PublishedMessages.Count, "There should be no published messages");
            
            Assert.IsNull(testQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the initial queue");
            Assert.IsNull(retryQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the retry queue");
            Assert.IsNull(failQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the failure queue");         
        }

        [TestMethod]
        public void TestFilterIncludedMessageDispatch()
        {
            recreateQueues();

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);

            ServiceBusRuntime serviceBusRuntime = new ServiceBusRuntime(testQueue, retryQueue, failQueue);

            serviceBusRuntime.ExponentialBackOff = false;
            serviceBusRuntime.RetryDelay = 1000;
            serviceBusRuntime.MaxRetries = 1;

            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/test", typeof(IContract), new WcfListener()));
            string message = "Publish this message";
            ContractImplementation ci = new ContractImplementation();
            ci.SetFailCount(0);

            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), new BooleanMessageFilter(true)));

            AutoResetEvent wait = new AutoResetEvent(false);
            serviceBusRuntime.MessageDelivered += (o, mdea) => { wait.Set(); };
            serviceBusRuntime.MessageDeliveryFailed += (o, mdfea) => { wait.Set(); };

            serviceBusRuntime.Start();

            serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));

            // Wait for delivery
            wait.WaitOne(TimeSpan.FromMinutes(1), false); // give it a minute

            serviceBusRuntime.Stop();

            Assert.AreEqual(1, ci.PublishedMessages.Count, "There should be one published message");
            Assert.AreEqual(message, ci.PublishedMessages.Dequeue(), "Message was not published properly");

            Assert.IsNull(testQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the initial queue");
            Assert.IsNull(retryQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the retry queue");
            Assert.IsNull(failQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the failure queue");
        }

        [TestMethod]
        public void TestSimpleMethodDispatch()
        {
            recreateQueues();

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);

            ServiceBusRuntime serviceBusRuntime = new ServiceBusRuntime(testQueue, retryQueue, failQueue);

            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/test", typeof(IContract), new WcfListener()));
            string message = "Publish this message";

            ContractImplementation ci = new ContractImplementation();
            ci.SetFailCount(0);
            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), new PassThroughMessageFilter()));

            AutoResetEvent wait = new AutoResetEvent(false);
            serviceBusRuntime.MessageDelivered += (o, mdea) => { wait.Set(); };
            serviceBusRuntime.MessageDeliveryFailed += (o, mdfea) => { wait.Set(); };

            serviceBusRuntime.Start();

            serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));

            // Wait for delivery
            wait.WaitOne(TimeSpan.FromMinutes(1), false); // give it a minute

            serviceBusRuntime.Stop();

            Assert.AreEqual(1, ci.PublishedMessages.Count, "There should be one published message");
            Assert.AreEqual(message, ci.PublishedMessages.Dequeue(), "Message was not publishe properly");

            Assert.IsNull(testQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the initial queue");
            Assert.IsNull(retryQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the retry queue");
            Assert.IsNull(failQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the failure queue");         
        }
        
        [TestMethod]
        public void DeliverABunchOfMessages()
        {
            recreateQueues();

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);

            ServiceBusRuntime serviceBusRuntime = new ServiceBusRuntime(testQueue, retryQueue, failQueue, SimpleServiceLocator.With(new PerformanceMonitorRuntimeService()));

            ContractImplementation ci = new ContractImplementation();
            ci.SetFailCount(0);

            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/test", typeof(IContract), new WcfListener()));
            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), new PassThroughMessageFilter()));


            int messageCount = 10000;

            DateTime start = DateTime.Now;

            CountdownLatch countDown = new CountdownLatch(messageCount);
            
            AutoResetEvent wait = new AutoResetEvent(false);
            serviceBusRuntime.MessageDelivered += (o, mdea) => { countDown.Tick();  };
            serviceBusRuntime.MessageDeliveryFailed += (o, mdfea) => { countDown.Tick(); };

            for (int i = 0; i < messageCount; i++)
            {
                string message = i.ToString();
                serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
            }

            serviceBusRuntime.Start();            

            
            bool[] results = new bool[messageCount];

            // Wait for delivery
            countDown.Handle.WaitOne(TimeSpan.FromMinutes(5), false); // give it 5 minutes

            DateTime end = DateTime.Now;

            System.Diagnostics.Trace.TraceInformation("Time to deliver "+messageCount+" = "+(end - start)); 
            serviceBusRuntime.Stop();

            while (ci.PublishedMessages.Count > 0)
            {
                results[Convert.ToInt32(ci.PublishedMessages.Dequeue())] = true;
            }

            for (int i = 0; i < messageCount; i++)
            {
                Assert.IsTrue(results[i], "Message is missing");
            }

            Assert.AreEqual(0, ci.PublishedMessages.Count, "There should be no extra messages");            
            Assert.IsNull(testQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the initial queue");
            Assert.IsNull(retryQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the retry queue");
            Assert.IsNull(failQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the failure queue");         
        }

        [TestMethod]
        public void DeliverABunchOfMessagesWithFailures()
        {            
            recreateQueues();

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);

            ServiceBusRuntime serviceBusRuntime = new ServiceBusRuntime(testQueue, retryQueue, failQueue, SimpleServiceLocator.With(new PerformanceMonitorRuntimeService()));

            serviceBusRuntime.ExponentialBackOff = false;
            serviceBusRuntime.RetryDelay = 1000;
            serviceBusRuntime.MaxRetries = 1000;
            
            ContractImplementation ci = new ContractImplementation();
            ci.SetFailCount(0);
            ci.SetFailInterval(10);

            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/test", typeof(IContract), new WcfListener()));
            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), new PassThroughMessageFilter()));


            int messageCount = 1000;

            DateTime start = DateTime.Now;

            CountdownLatch countDown = new CountdownLatch(messageCount);

            
            AutoResetEvent wait = new AutoResetEvent(false);
            serviceBusRuntime.MessageDelivered += (o, mdea) => { countDown.Tick(); };

            serviceBusRuntime.Start();


            for (int i = 0; i < messageCount; i++)
            {
                string message = i.ToString();
                serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
            }

            bool[] results = new bool[messageCount];

            // Wait for delivery
            countDown.Handle.WaitOne(TimeSpan.FromMinutes(5), false); // give it 5 minutes

            DateTime end = DateTime.Now;

            System.Diagnostics.Trace.TraceInformation("Time to deliver " + messageCount + " = " + (end - start));
            serviceBusRuntime.Stop();

            while (ci.PublishedMessages.Count > 0)
            {
                results[Convert.ToInt32(ci.PublishedMessages.Dequeue())] = true;
            }

            for (int i = 0; i < messageCount; i++)
            {
                Assert.IsTrue(results[i], "Message is missing");
            }

            Assert.AreEqual(0, ci.PublishedMessages.Count, "There should be no extra messages");
            Assert.IsNull(testQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the initial queue");
            Assert.IsNull(retryQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the retry queue");
            Assert.IsNull(failQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the failure queue");
        
        }

        [TestMethod]
        public void TestRetryQueue()
        {
            recreateQueues();

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);

            ServiceBusRuntime serviceBusRuntime = new ServiceBusRuntime(testQueue, retryQueue, failQueue);

            serviceBusRuntime.ExponentialBackOff = false;
            serviceBusRuntime.RetryDelay = 1000;
            serviceBusRuntime.MaxRetries = 1;

            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/test", typeof(IContract), new WcfListener()));
            string message = "Publish this message";
            ContractImplementation ci = new ContractImplementation();
            ci.SetFailCount(1);
            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), new PassThroughMessageFilter()));


            CountdownLatch latch = new CountdownLatch(2);

            bool failFirst = false;
            bool deliverSecond = false;            

            serviceBusRuntime.MessageDelivered += (o, mdea) => { 
                int tick; if ((tick = latch.Tick()) == 0) deliverSecond = true; Console.WriteLine("Tick deliver "+tick); 
            };
            serviceBusRuntime.MessageDeliveryFailed += (o, mdfea) => { 
                int tick; if ((tick = latch.Tick()) == 1) failFirst = true; Console.WriteLine("Tick fail "+tick); 
            };

            serviceBusRuntime.Start();

            serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));

            // Wait for delivery
            latch.Handle.WaitOne(TimeSpan.FromMinutes(1), false); // give it a minute

            serviceBusRuntime.Stop();

            Assert.AreEqual(1, ci.PublishedMessages.Count, "There should be one published message");
            Assert.AreEqual(message, ci.PublishedMessages.Dequeue(), "Message was not published properly");

            Assert.AreEqual(true, failFirst, "Call did not fail first");
            Assert.AreEqual(true, deliverSecond, "Call did not deliver on retry attempt");

            Assert.IsNull(testQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the initial queue");
            Assert.IsNull(retryQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the retry queue");
            Assert.IsNull(failQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the failure queue");
        }


        [TestMethod]
        public void TestFailQueue()
        {
            recreateQueues();

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);
            
            ServiceBusRuntime serviceBusRuntime = new ServiceBusRuntime(testQueue, retryQueue, failQueue);

            serviceBusRuntime.ExponentialBackOff = false;
            serviceBusRuntime.RetryDelay = 1000;
            serviceBusRuntime.MaxRetries = 1;

            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/test", typeof(IContract), new WcfListener()));
            string message = "Publish this message";
            ContractImplementation ci = new ContractImplementation();
            ci.SetFailCount(3);
            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), new PassThroughMessageFilter()));

            CountdownLatch latch = new CountdownLatch(3);
            
            serviceBusRuntime.MessageDelivered += (o, mdea) =>
            {
                latch.Tick();
            };
            serviceBusRuntime.MessageDeliveryFailed += (o, mdfea) =>
            {
                latch.Tick();
            };

            serviceBusRuntime.Start();

            serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));

            // Wait for delivery
            latch.Handle.WaitOne(TimeSpan.FromMinutes(1), false); // give it a minute

            serviceBusRuntime.Stop();

            Assert.AreEqual(0, ci.PublishedMessages.Count, "There should be no published message");

            Assert.IsNull(testQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the initial queue");
            Assert.IsNull(retryQueue.Peek(TimeSpan.FromSeconds(1)), "There should be no messages in the retry queue");
            Assert.IsNotNull(failQueue.Peek(TimeSpan.FromSeconds(1)), "There should be a message in the failure queue");
        }

    }
}
