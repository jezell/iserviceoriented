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

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "NamedPipeClient", "net.pipe://localhost/remotehello", typeof(IContract), new WcfProxyDispatcher(), null);
            
            string message = "blah blah test test";

            WcfProxyDispatcher contractDispatcher = new WcfProxyDispatcher(endpoint);
            contractDispatcher.Dispatch(new MessageDelivery(endpoint.Id, typeof(IContract), "http://tempuri.org/PublishThis", message, 3, new MessageDeliveryContext()));

            Assert.AreEqual(1, ci.PublishedCount);
            Assert.AreEqual(message, ci.PublishedMessages[0]);

            host.Close();
        }

        [Test]
        public void Can_Dispatch_Raw_Messages_To_Pass_Through_Endpoint()
        {
            PassThroughService pts = new PassThroughService();
            ServiceHost host = new ServiceHost(pts);
            host.Open();

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "PassThroughClient", "net.pipe://localhost/passthrough", typeof(IPassThroughServiceContract), new WcfProxyDispatcher(), null);

            string action = "http://someaction";
            string body = "this is a test";

            pts.Validator = (msg) => { Assert.AreEqual(msg.Headers.Action, action); Assert.AreEqual(msg.GetBody<string>(), body); };

            Message message = Message.CreateMessage(MessageVersion.Default, action, body);

            WcfProxyDispatcher contractDispatcher = new WcfProxyDispatcher(endpoint);
            contractDispatcher.Dispatch(new MessageDelivery(endpoint.Id, typeof(IPassThroughServiceContract), action, message, 3, new MessageDeliveryContext()));

            Assert.AreEqual(1, pts.PublishedCount);

            host.Close();
        }

        [Test]
        public void Can_Dispatch_Raw_Messages_To_Typed_Endpoint()
        {
            ContractImplementation ci = new ContractImplementation();
            ServiceHost host = new ServiceHost(ci);
            host.Open();

            SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", "PassThroughClient", "net.pipe://localhost/remotehello", typeof(IPassThroughServiceContract), new WcfProxyDispatcher(), null);

            string action = "http://tempuri.org/IContract/PublishThis";
            string body = "blah blah test test";

            XmlDocument document = new XmlDocument();
            document.LoadXml("<PublishThis xmlns='http://tempuri.org/'><message>"+body+"</message></PublishThis>");
            Message message = Message.CreateMessage(MessageVersion.Default, action, new XmlNodeReader(document));
            WcfProxyDispatcher contractDispatcher = new WcfProxyDispatcher(endpoint);
            contractDispatcher.Dispatch(new MessageDelivery(endpoint.Id, typeof(IPassThroughServiceContract), action, message, 3, new MessageDeliveryContext()));

            Assert.AreEqual(1, ci.PublishedCount);
            Assert.AreEqual(body, ci.PublishedMessages[0]);

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
