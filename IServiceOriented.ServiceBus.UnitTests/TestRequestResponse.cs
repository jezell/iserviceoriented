using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestRequestResponse
    {
        public TestRequestResponse()
        {
        }


        [Test]
        public void Echo()
        {
            using (ServiceBusRuntime runtime = new ServiceBusRuntime(SimpleServiceLocator.With(new QueuedDeliveryCore(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue()))))
            {
                CEcho echo = new CEcho();

                SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", null, null, typeof(void), new MethodDispatcher(echo, false), new IgnoreReplyFilter());
                runtime.Subscribe(replyEndpoint);
                runtime.Start();
                try
                {
                    string message = "echo this";

                    MessageDelivery[] output = null;
                    runtime.Publish(new PublishRequest(typeof(void), "Echo", message), PublishWait.Timeout, TimeSpan.FromSeconds(10), out output);

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
