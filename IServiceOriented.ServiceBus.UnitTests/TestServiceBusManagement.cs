using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Services;
using IServiceOriented.ServiceBus.Listeners;
using IServiceOriented.ServiceBus.Dispatchers;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestServiceBusManagement
    {
        public TestServiceBusManagement()
        {
        }

        [TestFixtureSetUp]
        public void Init()
        {
            
        }

        [TestFixtureTearDown]
        public void Uninit()
        {
            
        }

        [Test]
        public void Can_Add_And_Remove_Subscriptions()
        {
            using(ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore(), new WcfManagementService()))
            {
                ServiceBusTest tester = new ServiceBusTest(runtime);
                tester.StartAndStop(() =>
                {
                    Service.Use<IServiceBusManagementService>(managementService =>
                        {
                            ListenerEndpoint endpoint = new ListenerEndpoint(Guid.NewGuid(), "name of endpoint", "NamedPipeListener", "net.pipe://test/someservice/", typeof(IContract), new WcfServiceHostListener());
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
        public void Can_Add_And_Remove_Listeners()
        {
            using (ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore(), new WcfManagementService()))
            {                
                ServiceBusTest tester = new ServiceBusTest(runtime);
                tester.StartAndStop(() =>
                {
                    Service.Use<IServiceBusManagementService>(managementService =>
                    {
                        SubscriptionEndpoint endpoint = new SubscriptionEndpoint(Guid.NewGuid(), "name of endpoint", "NamedPipeClient", "net.pipe://test/someservice/", typeof(IContract), new WcfProxyDispatcher(), null);
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
