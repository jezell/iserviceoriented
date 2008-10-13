using System;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Dispatchers;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestWcfDispatcher
    {
        public TestWcfDispatcher()
        {
        }
       

        [Test]
        public void Can_Dispatch_To_ServiceHost()
        {            
            ContractImplementation ci = new ContractImplementation();
            ServiceHost host = new ServiceHost(ci);
            host.Open();

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "NamedPipeClient", "net.pipe://localhost/remotehello", typeof(IContract), new WcfDispatcher(), null);
            
            string message = "blah blah test test";

            WcfDispatcher contractDispatcher = new WcfDispatcher(endpoint);
            contractDispatcher.Dispatch(new MessageDelivery(endpoint.Id, typeof(IContract), "PublishThis", message, 3, new MessageDeliveryContext()));

            Assert.AreEqual(1, ci.PublishedCount);
            Assert.AreEqual(message, ci.PublishedMessages[0]);

            host.Close();
        }
    }
}
