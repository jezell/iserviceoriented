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

    [ServiceContract]
    public interface ISendComplexData
    {
        [OperationContract]
        void Send(ComplexData data);
    }


    [ServiceContract]
    public interface ISendMessageContract
    {
        [OperationContract]
        void Send(MessageContractMessage message);
    }

    [MessageContract]
    public class MessageContractMessage
    {
        [MessageBodyMember]
        public string Data;
    }

    [ServiceContract]
    public interface ISendDataContract
    {
        [OperationContract]
        void Send(DataContractMessage message);
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
            if (Config.TestQueuePath == null || Config.RetryQueuePath == null || Config.FailQueuePath == null)
            {
                Assert.Ignore("Test msmq queues not configured, skipping msmq tests");
            }
            else
            {
                recreateQueue();
            }
            
        }
        void recreateQueue()
        {
            // Delete test queues if they already exist

            if (MsmqMessageDeliveryQueue.Exists(Config.TestQueuePath))
            {
                MsmqMessageDeliveryQueue.Delete(Config.TestQueuePath);
            }            
            // Create test queue
            MsmqMessageDeliveryQueue.Create(Config.TestQueuePath);
        }

        [Test]
        public void Enqueue_Transactions_Abort_Properly()
        {
            recreateQueue();

            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath, new MessageDeliveryFormatter(typeof(IContract)));

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, typeof(IContract), "PublishThis", "randomMessageData", 3, new MessageDeliveryContext());
            Assert.IsNull(queue.Peek(TimeSpan.FromSeconds(1))); // make sure queue is null before starting

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
        }


        [Test]
        public void Enqueue_Transactions_Commit_Properly()
        {
            recreateQueue();

            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath, new MessageDeliveryFormatter(typeof(IContract)));
            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, typeof(IContract), "PublishThis", "randomMessageData", 3, new MessageDeliveryContext());
            Assert.IsNull(queue.Peek(TimeSpan.FromSeconds(1))); // Make sure queue is null

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
        }

        [Test]
        public void Dequeue_Transactions_Abort_Properly()
        {
            recreateQueue();

            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath, new MessageDeliveryFormatter(typeof(IContract)));
            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, typeof(IContract), "PublishThis", "randomMessageData", 3, new MessageDeliveryContext());
            Assert.IsNull(queue.Peek(TimeSpan.FromSeconds(1))); // Make sure queue is null

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
            }

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery dequeued = queue.Dequeue(TimeSpan.FromSeconds(5));
                Assert.IsNotNull(dequeued);
            }
        }

        [Test]
        public void Dequeue_Transactions_Commit_Properly()
        {
            recreateQueue();

            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath, new MessageDeliveryFormatter(typeof(IContract)));
            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, typeof(IContract), "PublishThis", "randomMessageData", 3, new MessageDeliveryContext());
            Assert.IsNull(queue.Peek(TimeSpan.FromSeconds(1))); // Make sure queue is null

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
        public void MessageContractFormatter_Can_Roundtrip_DataContract()
        {
            recreateQueue();

            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath, new MessageDeliveryFormatter(typeof(ISendDataContract)));
            string action = "http://tempuri.org/Send";
            DataContractMessage outgoing = new DataContractMessage() { Data = "This is a test" };

            Dictionary<string, object> context = new Dictionary<string, object>();
            context.Add("test", "value");

            MessageDelivery outgoingDelivery = new MessageDelivery(Guid.NewGuid(), typeof(ISendDataContract), action, outgoing, 5, new MessageDeliveryContext(context));
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
        public void MessageContractFormatter_Can_Roundtrip_MessageContract()
        {
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath, new MessageDeliveryFormatter(typeof(ISendMessageContract)));
            string action = "http://tempuri.org/Send";
            MessageContractMessage outgoing = new MessageContractMessage() { Data = "This is a test" };

            Dictionary<string, object> context = new Dictionary<string, object>();
            context.Add("test", "value");

            MessageDelivery outgoingDelivery = new MessageDelivery(Guid.NewGuid(), typeof(ISendMessageContract), action, outgoing, 5, new MessageDeliveryContext(context));
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
        public void Can_Deliver_Complex_Message()
        {
            testMessageDelivery(typeof(ISendComplexData), "testAction", new ComplexData(91302,1120));
        }

        [Test]
        public void Can_Deliver_Simple_Message()
        {
            testMessageDelivery(typeof(IContract), "PublishThis", "testMessageData");
        }
        
        
        void testMessageDelivery(Type interfaceType, string messageAction, object messageData)
        {
            MsmqMessageDeliveryQueue queue = new MsmqMessageDeliveryQueue(Config.TestQueuePath, new MessageDeliveryFormatter(interfaceType));

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "SubscriptionName", "http://localhost/test", "SubscriptionConfigName", interfaceType, new WcfDispatcher(), new PassThroughMessageFilter());

            MessageDelivery enqueued = new MessageDelivery(endpoint.Id, interfaceType, messageAction, messageData, 3, new MessageDeliveryContext());
                
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
