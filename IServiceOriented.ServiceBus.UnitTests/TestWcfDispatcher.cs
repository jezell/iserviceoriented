using System;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Dispatchers;
using System.ServiceModel.Channels;
using System.Threading;
using System.Xml;
using IServiceOriented.ServiceBus.Delivery;

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

            using (ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore()))
            {
                WcfProxyDispatcher contractDispatcher = new WcfProxyDispatcher();

                SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "NamedPipeClient", "net.pipe://localhost/remotehello", typeof(IContract), contractDispatcher, null);

                runtime.Subscribe(endpoint);

                runtime.Start();


                string message = "blah blah test test";


                contractDispatcher.Dispatch(new MessageDelivery(endpoint.Id, typeof(IContract), "http://tempuri.org/PublishThis", message, 3, new MessageDeliveryContext()));

                Assert.AreEqual(1, ci.PublishedCount);
                Assert.AreEqual(message, ci.PublishedMessages[0]);

                runtime.Stop();
                host.Close();
            }
        }

        [Test]
        public void Can_Dispatch_Raw_Messages_To_Pass_Through_Endpoint()
        {
            PassThroughService pts = new PassThroughService();
            ServiceHost host = new ServiceHost(pts);
            host.Open();

            WcfProxyDispatcher contractDispatcher = new WcfProxyDispatcher();                
            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "PassThroughClient", "net.pipe://localhost/passthrough", typeof(IPassThroughServiceContract), contractDispatcher, new PassThroughMessageFilter());

            string action = "http://someaction";
            string body = "this is a test";

            pts.Validator = (msg) => { Assert.AreEqual(msg.Headers.Action, action); Assert.AreEqual(msg.GetBody<string>(), body); };

            Message message = Message.CreateMessage(MessageVersion.Default, action, body);

            using (ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore()))
            {
                runtime.Subscribe(endpoint);

                runtime.Start();

                contractDispatcher.Dispatch(new MessageDelivery(endpoint.Id, typeof(IPassThroughServiceContract), action, message, 3, new MessageDeliveryContext()));

                runtime.Stop();
            }

            Assert.AreEqual(1, pts.PublishedCount);

            host.Close();
        }

        [Test]
        public void Can_Dispatch_Raw_Messages_To_Typed_Endpoint()
        {
            ContractImplementation ci = new ContractImplementation();
            ServiceHost host = new ServiceHost(ci);
            host.Open();

            WcfProxyDispatcher contractDispatcher = new WcfProxyDispatcher();
             
            using (ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore()))
            {

                SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "PassThroughClient", "net.pipe://localhost/remotehello", typeof(IPassThroughServiceContract), contractDispatcher, null);

                runtime.Subscribe(endpoint);

                runtime.Start();


                string action = "http://tempuri.org/IContract/PublishThis";
                string body = "blah blah test test";

                XmlDocument document = new XmlDocument();
                document.LoadXml("<PublishThis xmlns='http://tempuri.org/'><message>" + body + "</message></PublishThis>");
                Message message = Message.CreateMessage(MessageVersion.Default, action, new XmlNodeReader(document));
                contractDispatcher.Dispatch(new MessageDelivery(endpoint.Id, typeof(IPassThroughServiceContract), action, message, 3, new MessageDeliveryContext()));

                Assert.AreEqual(1, ci.PublishedCount);
                Assert.AreEqual(body, ci.PublishedMessages[0]);

                runtime.Stop();
            }
            
            host.Close();
        }
    }

    [ServiceBehavior(ConfigurationName="PassThroughListener", InstanceContextMode=InstanceContextMode.Single)]
    public class PassThroughService : IPassThroughServiceContract
    {
        public void Send(Message message)
        {
            if (Validator != null) Validator(message);
            Interlocked.Increment(ref _count);
        }

        public Action<Message> Validator;

        int _count;

        public int PublishedCount
        {
            get
            {
                return _count;
            }
        }
    }
}
