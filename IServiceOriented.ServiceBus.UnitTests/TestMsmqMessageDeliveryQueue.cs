using System;
using System.Transactions;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Collections;

using System.ServiceModel;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Delivery.Formatters;
using IServiceOriented.ServiceBus.Dispatchers;

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

    [MessageContract]
    public class MessageContractMessage
    {
        [MessageBodyMember]
        public string Data;
    }


    [DataContract]
    public class DataContractMessage
    {
        [DataMember]
        public string Data;
    }

    [TestFixture]
    public class TestMsmqMessageDeliveryQueue
    {
        public TestMsmqMessageDeliveryQueue()
        {
            
        }

        
        [TestFixtureSetUp]
        public void Initialize()
        {
            MessageDelivery.RegisterKnownType(typeof(ComplexData));
            

            // Delete test queues if they already exist
            try
            {
                MsmqMessageDeliveryQueue.Delete(Config.TestQueuePath);
            }
            catch
            {
            }        

            // Create test queue
            MsmqMessageDeliveryQueue.Create(Config.TestQueuePath);
            
        }

        [Test]
        public void TestTransactionSupport()
        {
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath);

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, typeof(MessageDelivery), "randomAction","randomMessageData", 3, new MessageDeliveryContext());
            Assert.IsNull(queue.Peek(TimeSpan.FromSeconds(1)));

            // Enqueue, but abort transaction
            using (TransactionScope ts = new TransactionScope())
            {
                queue.Enqueue(enqueued);
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(5));
                Assert.IsNull(dequeued);
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
                Assert.IsNotNull(dequeued);
                Assert.AreEqual(dequeued.MessageId, enqueued.MessageId);
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(10));
                Assert.IsNotNull(dequeued);
                Assert.AreEqual(dequeued.MessageId, enqueued.MessageId);
                ts.Complete();
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(5));
                Assert.IsNull(dequeued);
            }
        }

        [Test]
        public void TestMessageContractFormatterWithDataContract()
        {
            MessageDeliveryFormatter formatter = new MessageDeliveryFormatter();
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath);
            queue.Formatter = formatter;
            string action = "http://test";
            DataContractMessage outgoing = new DataContractMessage() { Data = "This is a test" };

            Dictionary<string, object> context = new Dictionary<string, object>();
            context.Add("test", "value");

            MessageDelivery outgoingDelivery = new MessageDelivery(Guid.NewGuid(), null, action, outgoing, 5, new MessageDeliveryContext(context));
            using (TransactionScope ts = new TransactionScope())
            {
                queue.Enqueue(outgoingDelivery);
                ts.Complete();
            }
            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery delivery = queue.Dequeue(TimeSpan.FromMinutes(1));
                Assert.AreEqual(typeof(DataContractMessage), delivery.Message.GetType());
                DataContractMessage incoming = (DataContractMessage)delivery.Message;
                Assert.AreEqual(incoming.Data, outgoing.Data);
                Assert.AreEqual(context["test"], delivery.Context["test"]);

                Assert.AreEqual(outgoingDelivery.Action, delivery.Action);
                Assert.AreEqual(outgoingDelivery.ContractType, delivery.ContractType);
                Assert.AreEqual(outgoingDelivery.MaxRetries, delivery.MaxRetries);
                Assert.AreEqual(outgoingDelivery.MessageId, delivery.MessageId);
                Assert.AreEqual(outgoingDelivery.RetryCount, delivery.RetryCount);
                Assert.AreEqual(outgoingDelivery.TimeToProcess, delivery.TimeToProcess);
                Assert.AreEqual(outgoingDelivery.SubscriptionEndpointId, delivery.SubscriptionEndpointId);
                ts.Complete();
            }
        }


        [Test]
        public void TestMessageContractFormatterWithMessageContract()
        {
            MessageDeliveryFormatter formatter = new MessageDeliveryFormatter();
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath);
            queue.Formatter = formatter;
            string action = "http://test";
            MessageContractMessage outgoing = new MessageContractMessage() { Data = "This is a test" };

            Dictionary<string, object> context = new Dictionary<string, object>();
            context.Add("test", "value");

            MessageDelivery outgoingDelivery = new MessageDelivery(Guid.NewGuid(), null, action, outgoing, 5, new MessageDeliveryContext(context));
            using (TransactionScope ts = new TransactionScope())
            {
                queue.Enqueue(outgoingDelivery);
                ts.Complete();
            }
            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery delivery = queue.Dequeue(TimeSpan.FromMinutes(1));
                Assert.AreEqual(typeof(MessageContractMessage), delivery.Message.GetType());
                MessageContractMessage incoming = (MessageContractMessage)delivery.Message;
                Assert.AreEqual(incoming.Data, outgoing.Data);
                Assert.AreEqual(context["test"], delivery.Context["test"]);

                Assert.AreEqual(outgoingDelivery.Action, delivery.Action);
                Assert.AreEqual(outgoingDelivery.ContractType, delivery.ContractType);
                Assert.AreEqual(outgoingDelivery.MaxRetries, delivery.MaxRetries);
                Assert.AreEqual(outgoingDelivery.MessageId, delivery.MessageId);
                Assert.AreEqual(outgoingDelivery.RetryCount, delivery.RetryCount);
                Assert.AreEqual(outgoingDelivery.TimeToProcess, delivery.TimeToProcess);
                Assert.AreEqual(outgoingDelivery.SubscriptionEndpointId, delivery.SubscriptionEndpointId);
                ts.Complete();
            }
        }
        [Test]
        public void TestWithComplexData()
        {
            testMessageDelivery("testAction", new ComplexData(91302,1120));
        }

        [Test]
        public void TestWithSimpleData()
        {
            testMessageDelivery("testAction", "testMessageData");
        }
        
        
        void testMessageDelivery(string messageAction, object messageData)
        {
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath);

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, typeof(MessageDelivery), messageAction, messageData, 3, new MessageDeliveryContext());
                
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
