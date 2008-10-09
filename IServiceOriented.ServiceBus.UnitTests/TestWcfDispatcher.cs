using System;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestWcfDispatcher
    {
        public TestWcfDispatcher()
        {
        }
       

        [Test]
        public void Dispatch()
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
