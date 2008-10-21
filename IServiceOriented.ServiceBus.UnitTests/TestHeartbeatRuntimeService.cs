using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using IServiceOriented.ServiceBus.Services;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Dispatchers;
using System.Threading;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestHeartbeatRuntimeService
    {
        [Test]
        public void Heartbeat_Timeout_Causes_Failure_Request()
        {
            using (HeartbeatRuntimeService heartbeatService = new HeartbeatRuntimeService())
            {
                using (ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore(), new TimerRuntimeService(), heartbeatService))
                {
                    heartbeatService.RegisterHeartbeat(new Heartbeat(Guid.NewGuid(), TimeSpan.FromSeconds(5), new PublishRequest(null, null, "BeatRequest"),
                                                        new PublishRequest(null, null, "Success"), new PublishRequest(null, null, "Failure"),
                                                        new PredicateMessageFilter(pr => (string)pr.Message == "BeatResponse"), TimeSpan.FromSeconds(1)));

                    AutoResetEvent failEvt = new AutoResetEvent(false);
                    AutoResetEvent successEvt = new AutoResetEvent(false);
                    AutoResetEvent responseEvt = new AutoResetEvent(false);
                    
                    /*runtime.Subscribe(new SubscriptionEndpoint("BeatResponse", null, null, null, new ActionDispatcher((se, md) => {
                        responseEvt.Set(); ThreadPool.QueueUserWorkItem(s => { Thread.Sleep(2000); runtime.PublishOneWay(new PublishRequest(null, null, "BeatResponse")); });
                    }), 
                        new PredicateMessageFilter(pr => (string)pr.Message == "BeatRequest")));*/
                    
                    runtime.Subscribe(new SubscriptionEndpoint("Success", null, null, null, new ActionDispatcher((se, md) => successEvt.Set()), new PredicateMessageFilter(pr => (string)pr.Message == "Success")));                    
                    runtime.Subscribe(new SubscriptionEndpoint("Failure", null, null, null, new ActionDispatcher((se, md) => failEvt.Set()), new PredicateMessageFilter(pr => (string)pr.Message == "Failure")));
                                            
                    ServiceBusTest tester = new ServiceBusTest(runtime);

                    tester.StartAndStop(() =>
                    {
                        if (!failEvt.WaitOne(TimeSpan.FromSeconds(10)))
                        {
                            if (!responseEvt.WaitOne(0))
                            {
                                Assert.Fail("The heartbeat request was never published.");
                            }
                            
                            if (successEvt.WaitOne(0))
                            {
                                Assert.Fail("The heartbeat success event was published instead of the failure event");
                            }
                            Assert.Fail("The heartbeat failure event was not published");
                        }

                        
                    });

                }
            }
            
        }

        [Test]
        public void Heartbeat_Requires_Timer_Service()
        {
            try
            {
                using (ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore(), new HeartbeatRuntimeService()))
                {
                    runtime.Start();
                }

                Assert.Fail();
            }
            catch(InvalidOperationException) // should not be able to start service bus without both timer runtime service
            {
                
            }
        }

        [Test]
        public void Heartbeat_Response_Causes_Success_Request()
        {
            using (HeartbeatRuntimeService heartbeatService = new HeartbeatRuntimeService())
            {
                using (ServiceBusRuntime runtime = new ServiceBusRuntime(new DirectDeliveryCore(), new TimerRuntimeService(), heartbeatService))
                {
                    heartbeatService.RegisterHeartbeat(new Heartbeat(Guid.NewGuid(), TimeSpan.FromSeconds(5), new PublishRequest(null, null, "BeatRequest"),
                                                        new PublishRequest(null, null, "Success"), new PublishRequest(null, null, "Failure"),
                                                        new PredicateMessageFilter(pr => (string)pr.Message == "BeatResponse"), TimeSpan.FromSeconds(1)));

                    AutoResetEvent responseEvt = new AutoResetEvent(false);
                    AutoResetEvent failEvt = new AutoResetEvent(false);
                    AutoResetEvent successEvt = new AutoResetEvent(false);

                    runtime.Subscribe(new SubscriptionEndpoint("BeatResponse", null, null, null, new ActionDispatcher((se, md) => { responseEvt.Set(); runtime.PublishOneWay(new PublishRequest(null, null, "BeatResponse")); }), new PredicateMessageFilter(pr => (string)pr.Message == "BeatRequest")));                     
                    runtime.Subscribe(new SubscriptionEndpoint("Success", null, null, null, new ActionDispatcher((se, md) => successEvt.Set()), new PredicateMessageFilter(pr => (string)pr.Message == "Success")));                    
                    runtime.Subscribe(new SubscriptionEndpoint("Failure", null, null, null, new ActionDispatcher((se, md) => failEvt.Set()), new PredicateMessageFilter(pr => (string)pr.Message == "Failure")));
                                            
                    ServiceBusTest tester = new ServiceBusTest(runtime);

                    tester.StartAndStop(() =>
                    {
                        if (!successEvt.WaitOne(TimeSpan.FromSeconds(10)))
                        {
                            if (!responseEvt.WaitOne(0))
                            {
                                Assert.Fail("The heartbeat request was never published.");
                            }                            
                            if (failEvt.WaitOne(0))
                            {
                                Assert.Fail("The heartbeat fail event was published instead of the success event");
                            }
                            Assert.Fail("The heartbeat success event was not published");
                        }
                    });

                }
            }
        }
    }
}
