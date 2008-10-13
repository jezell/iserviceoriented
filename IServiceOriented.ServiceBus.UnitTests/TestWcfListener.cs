using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Listeners;
using IServiceOriented.ServiceBus.Dispatchers;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestWcfListener
    {
        [Test]
        public void WcfListener_Publishes_Incoming_Messages_Properly()
        {
            ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore());
            runtime.AddListener(new ListenerEndpoint("test", "NamedPipeListener", "net.pipe://localhost/remotehello", typeof(IContract), new WcfListener()));
           
            string message = "This is a test";

            runtime.Subscribe(new SubscriptionEndpoint("test subscription", null, null, typeof(IContract), new ActionDispatcher((se, md) => { Assert.AreEqual(message, (string)md.Message); }), new PassThroughMessageFilter()));

            ServiceBusTest tester = new ServiceBusTest(runtime);
            tester.WaitForDeliveries(1, TimeSpan.FromSeconds(5), () =>
            {
                Service.Use<IContract>("NamedPipeClient", contract =>
                {
                    contract.PublishThis(message);
                });
            });

        }
    }
}
