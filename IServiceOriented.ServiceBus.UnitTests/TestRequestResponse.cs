using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Dispatchers;
using IServiceOriented.ServiceBus.Delivery.Formatters;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestRequestResponse
    {
        public TestRequestResponse()
        {
        }

        public void TEst()
        {
            ContractDescription description = ContractDescription.GetContract(typeof(ISendMessageContract));
            Console.WriteLine(description);
        }


        [Test]
        public void QueuedDeliveryCore_Publishes_Response_Messages_For_TwoWay_Operation()
        {
            System.Messaging.IMessageFormatter binaryFormatter = new System.Messaging.BinaryMessageFormatter();
            using (ServiceBusRuntime runtime = Create.BinaryMsmqRuntime())
            {
                CEcho echo = new CEcho();

                SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", null, null, typeof(void), new MethodDispatcher(echo, false), new IgnoreReplyFilter());
                runtime.Subscribe(replyEndpoint);
                runtime.Start();
                try
                {
                    string message = "echo this";

                    MessageDelivery[] output = runtime.PublishTwoWay(new PublishRequest(typeof(void), "Echo", message), TimeSpan.FromSeconds(10));

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
