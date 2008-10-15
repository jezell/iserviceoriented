using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Listeners;
using IServiceOriented.ServiceBus.Dispatchers;
using NUnit.Framework;
using System.IO;
using IServiceOriented.ServiceBus.Delivery.Formatters;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestFileSystemDispatchersAndListeners
    {
        public TestFileSystemDispatchersAndListeners()
        {
        }

        [TestFixtureSetUp]
        public void Init()
        {
            if (Config.IncomingFilePath == null || Config.ProcessedFilePath == null)
            {
                Assert.Ignore("No incoming or processed file path configured. Ignoring tests");
            }
            else
            {
                if (Directory.Exists(Config.IncomingFilePath)) Directory.Delete(Config.IncomingFilePath, true);
                if (Directory.Exists(Config.ProcessedFilePath)) Directory.Delete(Config.ProcessedFilePath, true);
            }
        }
        
        [Test]
        public void FileSystemDispatcher_Can_Send_To_FileSystemListener()
        {
            ServiceBusRuntime dispatchRuntime = new ServiceBusRuntime(new DirectDeliveryCore());
            var subscription = new SubscriptionEndpoint(Guid.NewGuid(), "File System Dispatcher", null, null, typeof(IContract), new FileSystemDispatcher(Config.IncomingFilePath), new PassThroughMessageFilter());
            dispatchRuntime.Subscribe(subscription);

            ServiceBusRuntime listenerRuntime = new ServiceBusRuntime(new DirectDeliveryCore());
            var listener = new ListenerEndpoint(Guid.NewGuid(), "File System Listener", null, null, typeof(IContract), new FileSystemListener(Config.IncomingFilePath, Config.ProcessedFilePath));
            listenerRuntime.AddListener(listener);
            listenerRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "Pass through", null, null, typeof(IContract), new ActionDispatcher((se, md) => { }), new PassThroughMessageFilter()));
            
            var dispatchTester = new ServiceBusTest(dispatchRuntime);
            var listenerTester = new ServiceBusTest(listenerRuntime);


            string message = "test this thing";

            dispatchTester.StartAndStop(() =>
            {                                
                listenerTester.WaitForDeliveries(1, TimeSpan.FromSeconds(10), ()=>
                {
                    dispatchRuntime.PublishOneWay(typeof(IContract), "PublishThis", message);
                });            
            });

            dispatchRuntime.Unsubscribe(subscription);
        }

        [Test]
        public void FileSystemDispatcher_Picks_Up_Existing_Messages()
        {
            
            ServiceBusRuntime dispatchRuntime = new ServiceBusRuntime(new DirectDeliveryCore());
            var subscription = new SubscriptionEndpoint(Guid.NewGuid(), "File System Dispatcher", null, null, typeof(IContract), new FileSystemDispatcher(Config.IncomingFilePath), new PassThroughMessageFilter());
            dispatchRuntime.Subscribe(subscription);

            ServiceBusRuntime listenerRuntime = new ServiceBusRuntime(new DirectDeliveryCore());
            var listener = new ListenerEndpoint(Guid.NewGuid(), "File System Listener", null, null, typeof(IContract), new FileSystemListener(Config.IncomingFilePath, Config.ProcessedFilePath));
            listenerRuntime.AddListener(listener);
            listenerRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "Pass through", null, null, typeof(IContract), new ActionDispatcher((se, md) => { }), new PassThroughMessageFilter()));

            var dispatchTester = new ServiceBusTest(dispatchRuntime);
            var listenerTester = new ServiceBusTest(listenerRuntime);


            string message = "test this thing";

            dispatchTester.StartAndStop(() =>
            {
                dispatchRuntime.PublishOneWay(typeof(IContract), "PublishThis", message);

                listenerTester.WaitForDeliveries(1, TimeSpan.FromSeconds(10), () =>
                {                    
                });
            });

            dispatchRuntime.Unsubscribe(subscription);
        }
    }
}
