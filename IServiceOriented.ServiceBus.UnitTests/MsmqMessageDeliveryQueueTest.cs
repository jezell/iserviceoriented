using System;
using System.Transactions;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [Serializable]
    [DataContract]
    public class ComplexData
    {
        public ComplexData()
        {
        }
        public ComplexData(int value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        [DataMember]
        public int Value1;
        [DataMember]
        public int Value2;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != typeof(ComplexData)) return false;

            ComplexData objData = (ComplexData)obj;

            return Value1 == objData.Value1 && Value2 == objData.Value2;
        }

        public override int GetHashCode()
        {
            return Value1.GetHashCode();
        }

        public static bool operator ==(ComplexData v1, ComplexData v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(ComplexData v1, ComplexData v2)
        {
            return !v1.Equals(v2);
        }
    }

    [TestClass]
    public class MsmqMessageDeliveryQueueTest
    {
        public MsmqMessageDeliveryQueueTest()
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
        
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            MessageDelivery.RegisterKnownType(typeof(ComplexData));
            

            // Delete test queues if they already exist
            try
            {                
                MsmqMessageDeliveryQueue.Delete(_testQueuePath);
            }
            catch
            {
            }
        

            // Create test queue
            MsmqMessageDeliveryQueue.Create(_testQueuePath);
            
        }
        
        [TestMethod]
        public void TestTransactionSupport()
        {
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(_testQueuePath);

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, "randomAction","randomMessageData", 3);
            Assert.IsNull(queue.Peek(TimeSpan.FromSeconds(1)), "Queue must be empty to start transaction test");

            // Enqueue, but abort transaction
            using (TransactionScope ts = new TransactionScope())
            {
                queue.Enqueue(enqueued);
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(5));
                Assert.IsNull(dequeued, "Aborted transaction should have prevented the message from being queued");
            }

            // Enqueue and commit transaction
            using (TransactionScope ts = new TransactionScope())
            {
                queue.Enqueue(enqueued);
                ts.Complete();
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(10));
                Assert.IsNotNull(dequeued, "Committed transaction should have queued the message");
                Assert.AreEqual(dequeued.MessageId, enqueued.MessageId, "Wrong message dequeued");
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(10));
                Assert.IsNotNull(dequeued, "Aborted transaction should not have dequeued the message");
                Assert.AreEqual(dequeued.MessageId, enqueued.MessageId, "Wrong message dequeued");
                ts.Complete();
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(5));
                Assert.IsNull(dequeued, "Committed transaction should have dequeued the message");
            }

        }

        [TestMethod]
        public void TestWithComplexData()
        {
            testMessageDelivery("testAction", new ComplexData(91302,1120));
        }

        [TestMethod]
        public void TestWithSimpleData()
        {
            testMessageDelivery("testAction", "testMessageData");
        }
        
        
        void testMessageDelivery(string messageAction, object messageData)
        {
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(_testQueuePath);

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, messageAction, messageData, 3);
                
            using (TransactionScope ts = new TransactionScope())
            {
                queue.Enqueue(enqueued);
                ts.Complete();
            }

            // Peek
            MessageDelivery dequeued = queue.Peek(TimeSpan.FromSeconds(30));
            Assert.IsNotNull(dequeued);
            Assert.AreEqual(enqueued.Action, dequeued.Action);
            Assert.AreEqual(enqueued.SubscriptionEndpointId, dequeued.SubscriptionEndpointId);
           
            using (TransactionScope ts = new TransactionScope())
            {   
                // Pull for real
                dequeued = queue.Dequeue(TimeSpan.FromSeconds(30));
                ts.Complete();
            }
            Assert.IsNotNull(dequeued); 
            Assert.AreEqual(enqueued.Action, dequeued.Action);
            Assert.AreEqual(enqueued.SubscriptionEndpointId, dequeued.SubscriptionEndpointId);
            
            // Should now be empty
            dequeued = queue.Peek(TimeSpan.FromSeconds(1));            
            Assert.IsNull(dequeued);
        }
    }
}
