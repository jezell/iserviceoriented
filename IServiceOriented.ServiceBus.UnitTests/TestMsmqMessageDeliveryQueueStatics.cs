using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestMsmqMessageDeliveryQueueStatics
    {
        [TestFixtureSetUp]
        public void Init()
        {
            if (Config.TestQueuePath == null)
            {
                Assert.Ignore("Test msmq queue not configured. Skipping");
            }
        }

        [Test]
        public void Can_Create_Queue()
        {
            if (MsmqMessageDeliveryQueue.Exists(Config.TestQueuePath))
            {
                MsmqMessageDeliveryQueue.Delete(Config.TestQueuePath);
            }
            MsmqMessageDeliveryQueue.Create(Config.TestQueuePath);
        }

        [Test]
        public void Can_Delete_Queue()
        {
            if (!MsmqMessageDeliveryQueue.Exists(Config.TestQueuePath))
            {
                MsmqMessageDeliveryQueue.Create(Config.TestQueuePath);
            }
            MsmqMessageDeliveryQueue.Delete(Config.TestQueuePath);
            Assert.IsFalse(MsmqMessageDeliveryQueue.Exists(Config.TestQueuePath));
        }

        [Test]
        public void Can_Check_Queue_Existence()
        {
            if (MsmqMessageDeliveryQueue.Exists(Config.TestQueuePath))
            {
                MsmqMessageDeliveryQueue.Delete(Config.TestQueuePath);
            }
            MsmqMessageDeliveryQueue.Create(Config.TestQueuePath);
            Assert.IsTrue(MsmqMessageDeliveryQueue.Exists(Config.TestQueuePath));
            MsmqMessageDeliveryQueue.Delete(Config.TestQueuePath);
            Assert.IsFalse(MsmqMessageDeliveryQueue.Exists(Config.TestQueuePath));
        }        

    }
}
