using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestServiceBusManagement
    {
        public TestServiceBusManagement()
        {
        }

        [Test]
        public void CanAddAndRemoveSubscription()
        {
            using(ServiceBusRuntime runtime = new ServiceBusRuntime(SimpleServiceLocator.With(new DirectDeliveryCore(), new WcfManagementService())))
            {
                ServiceBusTest tester = new ServiceBusTest(runtime);
                tester.StartAndStop(() =>
                {
                    Service.Use<IServiceBusManagementService>(managementService =>
                        {
                            ListenerEndpoint endpoint = new ListenerEndpoint(Guid.NewGuid(), "name of endpoint", "NamedPipeListener", "net.pipe://test/someservice/", typeof(IContract), new WcfListener());
                            managementService.Listen(endpoint);

                            ListenerEndpoint added = managementService.ListListeners().First();
                            tester.AssertEqual(endpoint, added);

                            managementService.StopListening(endpoint.Id);
                            Assert.IsEmpty(managementService.ListListeners());
                        });
                });
            }
        }

        [Test]
        public void CanAddAndRemoveListener()
        {
            using (ServiceBusRuntime runtime = new ServiceBusRuntime(SimpleServiceLocator.With(new DirectDeliveryCore(), new WcfManagementService())))
            {
                MessageDelivery.RegisterKnownType(typeof(PassThroughMessageFilter));

                ServiceBusTest tester = new ServiceBusTest(runtime);
                tester.StartAndStop(() =>
                {
                    Service.Use<IServiceBusManagementService>(managementService =>
                    {
                        SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "name of endpoint", "NamedPipeClient", "net.pipe://test/someservice/", typeof(IContract), new WcfDispatcher(), new PassThroughMessageFilter());
                        managementService.Subscribe(endpoint);

                        SubscriptionEndpoint added = managementService.ListSubscribers().First();
                        tester.AssertEqual(endpoint, added);

                        managementService.Unsubscribe(endpoint.Id);
                        Assert.IsEmpty(managementService.ListSubscribers());
                    });
                });
            }
        }

        
    }
}
