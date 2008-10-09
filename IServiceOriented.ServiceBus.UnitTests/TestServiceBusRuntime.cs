using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Threading;
using IServiceOriented.ServiceBus.Threading;
using System.Collections.ObjectModel;

namespace IServiceOriented.ServiceBus.UnitTests
{
    
    public class ServiceBusTest
    {
        public ServiceBusTest(ServiceBusRuntime runtime)
        {
            serviceBusRuntime = runtime;
        }

        ServiceBusRuntime serviceBusRuntime;

        public void VerifyQueuesEmpty()
        {
            QueuedDeliveryCore core = serviceBusRuntime.ServiceLocator.GetInstance<QueuedDeliveryCore>();
            Assert.IsNull(core.RetryQueue.Peek(TimeSpan.FromMilliseconds(500)));
            Assert.IsNull(core.FailureQueue.Peek(TimeSpan.FromMilliseconds(500)));
            Assert.IsNull(core.MessageDeliveryQueue.Peek(TimeSpan.FromMilliseconds(500)));
        }

        public void OnlyRetryOnce()
        {
            serviceBusRuntime.ServiceLocator.GetInstance<QueuedDeliveryCore>().ExponentialBackOff = false;
            serviceBusRuntime.ServiceLocator.GetInstance<QueuedDeliveryCore>().RetryDelay = 1000;
            serviceBusRuntime.MaxRetries = 1;
        }

        public void AddTestListener()
        {
            serviceBusRuntime.AddListener(new ListenerEndpoint(Guid.NewGuid(), "test", "NamedPipeListener", "net.pipe://localhost/servicebus/testlistener", typeof(IContract), new WcfListener()));
        }

        public void AddTestSubscription(ContractImplementation ci, MessageFilter messageFilter)
        {
            serviceBusRuntime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "subscription", "", "", typeof(IContract), new MethodDispatcher(ci), messageFilter));
        }

        public void StartAndStop(Action inner)
        {
            serviceBusRuntime.Start();
            
            inner();
            
            serviceBusRuntime.Stop();            
        }

        public void WaitForDeliveriesOrFailures(int deliveryCount, TimeSpan timeout, Action inner)
        {
            waitForDeliveries(deliveryCount, true, timeout, inner);
        }

        public void WaitForDeliveries(int deliveryCount, TimeSpan timeout, Action inner)
        {
            waitForDeliveries(deliveryCount, false, timeout, inner);
        }

        void waitForDeliveries(int deliveryCount, bool includeFailures, TimeSpan timeout, Action inner)
        {
            using (CountdownLatch latch = new CountdownLatch(deliveryCount))
            {

                EventHandler<MessageDeliveryEventArgs> delivered = (o, mdea) => {  latch.Tick(); };
                EventHandler<MessageDeliveryFailedEventArgs> deliveryFailed = (o, mdfa) => { latch.Tick(); };
                
                serviceBusRuntime.MessageDelivered += delivered;
                if(includeFailures) serviceBusRuntime.MessageDeliveryFailed += deliveryFailed;

                try
                {
                    StartAndStop(() =>
                    {
                        inner();

                        latch.Handle.WaitOne(timeout);
                    });
                }
                finally
                {
                    serviceBusRuntime.MessageDelivered -= delivered;
                    if (includeFailures) serviceBusRuntime.MessageDeliveryFailed -= deliveryFailed;
                }


                
            }
        }

    }

    [TestFixture]
    public class TestServiceBusRuntime
    {
        public TestServiceBusRuntime()
        {            
        }

        
        
        [Test]
        public void TestFilterExcludedMessageDispatch()
        {            
            using (var serviceBusRuntime = Create.MsmqRuntime())
            {
                ServiceBusTest tester = new ServiceBusTest(serviceBusRuntime);
   
                string message = "Publish this message";

                ContractImplementation ci = new ContractImplementation();
                ci.SetFailCount(1);

                tester.OnlyRetryOnce();
                
                tester.AddTestListener();
                tester.AddTestSubscription(ci, new BooleanMessageFilter(false));

                
                tester.WaitForDeliveriesOrFailures(1, TimeSpan.FromSeconds(5), () =>
                {
                    serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
                });
            
                Assert.AreEqual(0, ci.PublishedCount);

                tester.VerifyQueuesEmpty(); 
            }
        }

        [Test]
        public void TestFilterIncludedMessageDispatch()
        {         
            using (var serviceBusRuntime = Create.MsmqRuntime())
            {
                ServiceBusTest tester = new ServiceBusTest(serviceBusRuntime);
                tester.OnlyRetryOnce();

                ContractImplementation ci = new ContractImplementation();
                ci.SetFailCount(0);

                tester.AddTestListener();
                tester.AddTestSubscription(ci, new BooleanMessageFilter(true));

                string message = "Publish this message";
                
                tester.WaitForDeliveries(1, TimeSpan.FromMinutes(1), ()=>
                {
                    serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
                });
                
                Assert.AreEqual(1, ci.PublishedCount);
                Assert.AreEqual(message, ci.PublishedMessages[0]);

                tester.VerifyQueuesEmpty(); 
            }
        }

        [Test]
        public void TestSimpleMethodDispatch()
        {
            using (var serviceBusRuntime = Create.MsmqRuntime())
            {
                ServiceBusTest tester = new ServiceBusTest(serviceBusRuntime);
                
                string message = "Publish this message";
                ContractImplementation ci = new ContractImplementation();
                ci.SetFailCount(0);

                tester.AddTestListener();
                tester.AddTestSubscription(ci, new PassThroughMessageFilter());

                tester.WaitForDeliveriesOrFailures(1, TimeSpan.FromSeconds(5), () =>
                {
                    serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
                });
            
                Assert.AreEqual(1, ci.PublishedCount);
                Assert.AreEqual(message, ci.PublishedMessages[0]);

                tester.VerifyQueuesEmpty(); 
            }
        }

        [Test]
        public void DeliverABunchOfMessages()
        {
            using (var serviceBusRuntime = Create.MsmqRuntime())
            {
                ServiceBusTest tester = new ServiceBusTest(serviceBusRuntime);
                
                ContractImplementation ci = new ContractImplementation();
                ci.SetFailCount(0);

                tester.AddTestListener();
                tester.AddTestSubscription(ci, new PassThroughMessageFilter());                

                int messageCount = 10000;
                
                DateTime start = DateTime.Now;


                tester.WaitForDeliveriesOrFailures(messageCount, TimeSpan.FromMinutes(1), () =>
                {
                    for (int i = 0; i < messageCount; i++)
                    {
                        string message = i.ToString();
                        serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
                    }                    
                });
            
                
                bool[] results = new bool[messageCount];
                
                DateTime end = DateTime.Now;

                System.Diagnostics.Trace.TraceInformation("Time to deliver messages "+messageCount+" = "+(end - start)); 
                
                Assert.AreEqual(messageCount, ci.PublishedCount);
                
                for(int i = 0; i < ci.PublishedCount; i++)
                {
                    int j = Convert.ToInt32(ci.PublishedMessages[i]);
                    results[j] = true;                    
                }

                for (int i = 0; i < messageCount; i++)
                {
                    Assert.IsTrue(results[i]);
                }

                tester.VerifyQueuesEmpty(); 
            }
        }

        [Test]
        public void DeliverABunchOfMessagesWithFailures()
        {
            using (var serviceBusRuntime = Create.MsmqRuntime())
            {
                ServiceBusTest tester = new ServiceBusTest(serviceBusRuntime);                
                tester.OnlyRetryOnce();

                ContractImplementation ci = new ContractImplementation();
                ci.SetFailCount(0);
                ci.SetFailInterval(10);

                QueuedDeliveryCore qc = serviceBusRuntime.ServiceLocator.GetInstance<QueuedDeliveryCore>();
                qc.RetryDelay = 1;
                
                tester.AddTestListener();
                tester.AddTestSubscription(ci, new PassThroughMessageFilter());

                int messageCount = 1000;

                DateTime start = DateTime.Now;

            
                tester.WaitForDeliveries(messageCount, TimeSpan.FromMinutes(1), () =>
                {
                    for (int i = 0; i < messageCount; i++)
                    {
                        string message = i.ToString();
                        serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
                    }
                });
                            

                bool[] results = new bool[messageCount];
                // Wait for delivery
                DateTime end = DateTime.Now;
                System.Diagnostics.Trace.TraceInformation("Time to deliver " + messageCount + " = " + (end - start));
                
                for (int i = 0; i < ci.PublishedCount; i++)
                {
                    results[Convert.ToInt32(ci.PublishedMessages[i])] = true;
                }

                for (int i = 0; i < messageCount; i++)
                {
                    Assert.IsTrue(results[i]);
                }

                Assert.AreEqual(messageCount, ci.PublishedCount);
                tester.VerifyQueuesEmpty(); 
            }
        
        }

        [Test]
        public void TestRetryQueue()
        {
            using (var serviceBusRuntime = Create.MsmqRuntime())
            {
                ServiceBusTest tester = new ServiceBusTest(serviceBusRuntime);

                string message = "Publish this message";
                ContractImplementation ci = new ContractImplementation();
                ci.SetFailCount(1);
                
                tester.OnlyRetryOnce();
                tester.AddTestListener();

                tester.AddTestSubscription(ci, new PassThroughMessageFilter());

                bool failFirst = false;
                bool deliverSecond = false;

                tester.StartAndStop(() =>
                {
                    CountdownLatch latch = new CountdownLatch(2);

                    serviceBusRuntime.MessageDelivered += (o, mdea) =>
                    {
                        int tick; if ((tick = latch.Tick()) == 0) deliverSecond = true; Console.WriteLine("Tick deliver " + tick);
                    };
                    serviceBusRuntime.MessageDeliveryFailed += (o, mdfea) =>
                    {
                        int tick; if ((tick = latch.Tick()) == 1) failFirst = true; Console.WriteLine("Tick fail " + tick);
                    };

                    serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));

                    // Wait for delivery
                    latch.Handle.WaitOne(TimeSpan.FromMinutes(1), false); // give it a minute

                });
                
                Assert.AreEqual(1, ci.PublishedCount);
                Assert.AreEqual(message, ci.PublishedMessages[0]);

                Assert.AreEqual(true, failFirst);
                Assert.AreEqual(true, deliverSecond);

                tester.VerifyQueuesEmpty(); 
            }
        }


        [Test]
        public void TestFailQueue()
        {
            using (var serviceBusRuntime = Create.MsmqRuntime())
            {
                ServiceBusTest tester = new ServiceBusTest(serviceBusRuntime);                
                tester.OnlyRetryOnce();

                string message = "Publish this message";
                ContractImplementation ci = new ContractImplementation();
                ci.SetFailCount(3);

                tester.AddTestListener();
                tester.AddTestSubscription(ci, new PassThroughMessageFilter());

                tester.WaitForDeliveriesOrFailures(3, TimeSpan.FromMinutes(.5), () =>
                {
                    serviceBusRuntime.Publish(new PublishRequest(typeof(IContract), "PublishThis", message));
                });                   
        
                Assert.AreEqual(0, ci.PublishedCount);

                MessageDelivery delivery = serviceBusRuntime.ServiceLocator.GetInstance<QueuedDeliveryCore>().FailureQueue.Dequeue(TimeSpan.FromSeconds(1));
                Assert.IsNotNull(delivery);

                tester.VerifyQueuesEmpty(); 
                Assert.AreEqual(3, ((IEnumerable<string>)delivery.Context[MessageDelivery.Exceptions]).Count());           
            }
        }

    }
}
