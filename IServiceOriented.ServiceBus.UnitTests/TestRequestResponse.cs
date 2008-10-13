using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Dispatchers;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestRequestResponse
    {
        public TestRequestResponse()
        {
        }


        [Test]
        public void QueuedDeliveryCore_Publishes_Response_Messages_For_TwoWay_Operation()
        {
            using (ServiceBusRuntime runtime = new ServiceBusRuntime(new QueuedDeliveryCore(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue())))
            {
                CEcho echo = new CEcho();

                SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", null, null, typeof(void), new MethodDispatcher(echo, false), new IgnoreReplyFilter());
                runtime.Subscribe(replyEndpoint);
                runtime.Start();
                try
                {
                    string message = "echo this";

                    MessageDelivery[] output = runtime.Publish(new PublishRequest(typeof(void), "Echo", message), PublishWait.Timeout, TimeSpan.FromSeconds(10));

                    Assert.IsNotNull(output);
                    Assert.AreEqual(1, output.Length);
                    Assert.AreEqual(message, (string)output[0].Message);
                }
                finally
                {
                    runtime.Stop();
                }
            }
        }

        public class IgnoreReplyFilter : MessageFilter
        {
            public override bool Include(PublishRequest request)
            {
                if (request.Context.ContainsKey(MessageDelivery.CorrelationId))
                {
                    return false;
                }
                return true;
            }
        }

        public class CEcho
        {
            public string Echo(string echo)
            {
                return echo;
            }
        }
        
    }
}
