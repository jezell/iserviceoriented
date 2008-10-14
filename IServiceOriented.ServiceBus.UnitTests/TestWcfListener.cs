using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Listeners;
using IServiceOriented.ServiceBus.Dispatchers;
using System.ServiceModel.Channels;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestWcfListener
    {
        [Test]
        public void WcfListener_Publishes_Incoming_Messages_Properly()
        {
            ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore());
            runtime.AddListener(new ListenerEndpoint("test", "NamedPipeListener", "net.pipe://localhost/remotehello", typeof(IContract), new WcfServiceHostListener()));
           
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

        [Test]
        public void WcfListener_Can_Listen_For_Raw_Messages()
        {
            ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore());
            runtime.AddListener(new ListenerEndpoint("test", "PassThroughListener", "net.pipe://localhost/passthrough", typeof(IPassThroughServiceContract), new WcfServiceHostListener()));

            string action = "http://someaction";
            string body = "some body";

            runtime.Subscribe(new SubscriptionEndpoint("test subscription", null, null, typeof(IContract), new ActionDispatcher((se, md) => { Assert.AreEqual(action, ((Message)md.Message).Headers.Action); Assert.AreEqual(body, ((Message)md.Message).GetBody<string>()); }), new PassThroughMessageFilter()));

            ServiceBusTest tester = new ServiceBusTest(runtime);
            tester.WaitForDeliveries(1, TimeSpan.FromSeconds(5), () =>
            {                
                Service.Use<IPassThroughServiceContract>("PassThroughClient", contract =>
                {
                    Message message = Message.CreateMessage(MessageVersion.Default, action, body);
                    contract.Send(message);
                });
            });
        }

       

    }
}
