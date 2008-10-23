using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Services;
using System.Threading;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestTimerRuntimeService
    {
        [Test]
        public void Action_Ticks_On_Intervals()
        {
            long count = 0;

            TimerRuntimeService timerService = new TimerRuntimeService();
            timerService.AddEvent(new TimerEvent(() => { Interlocked.Increment(ref count); }, TimeSpan.FromMilliseconds(1000)));
            
            DeliveryCore deliveryCore = new DirectDeliveryCore();

            using (ServiceBusRuntime runtime = new ServiceBusRuntime(deliveryCore, timerService))
            {
                runtime.Start();

                Thread.Sleep(9100);

                long curCount = Interlocked.Read(ref count);
                
                runtime.Stop();

                Assert.AreEqual(9, curCount);                
            }
        }

        [Test]
        public void Action_Starts_On_Start()
        {
            long count = 0;

            TimerRuntimeService timerService = new TimerRuntimeService();
            timerService.AddEvent(new TimerEvent(() => { Interlocked.Increment(ref count); }, TimeSpan.FromMilliseconds(100)));

            DeliveryCore deliveryCore = new DirectDeliveryCore();

            using (ServiceBusRuntime runtime = new ServiceBusRuntime(deliveryCore, timerService))
            {
                Thread.Sleep(500);

                long curCount = Interlocked.Read(ref count);
                Assert.AreEqual(0, curCount);
                
                runtime.Start();

                Thread.Sleep(500);

                curCount = Interlocked.Read(ref count);

                runtime.Stop();

                Assert.GreaterOrEqual(curCount,0);                
            }
        }

        [Test]
        public void Action_Stops_On_Stop()
        {
            long count = 0;

            TimerRuntimeService timerService = new TimerRuntimeService();
            timerService.AddEvent(new TimerEvent(() => { Interlocked.Increment(ref count); }, TimeSpan.FromMilliseconds(1000)));

            DeliveryCore deliveryCore = new DirectDeliveryCore();

            using (ServiceBusRuntime runtime = new ServiceBusRuntime(deliveryCore, timerService))
            {                
                runtime.Start();

                Thread.Sleep(1100);

                long beforeStopCount = Interlocked.Read(ref count);

                runtime.Stop();

                Assert.GreaterOrEqual(beforeStopCount, 0);

                Thread.Sleep(1100);

                long afterStopCount = Interlocked.Read(ref count);

                Assert.GreaterOrEqual(beforeStopCount, afterStopCount);
            }
        }

        [Test]
        public void Event_Auto_Starts_When_Running()
        {
            long count = 0;

            TimerRuntimeService timerService = new TimerRuntimeService();
            var evt = new TimerEvent(() => { Interlocked.Increment(ref count); }, TimeSpan.FromMilliseconds(100));
            
            DeliveryCore deliveryCore = new DirectDeliveryCore();

            using (ServiceBusRuntime runtime = new ServiceBusRuntime(deliveryCore, timerService))
            {                                
                runtime.Start();

                Thread.Sleep(500);

                long curCount = Interlocked.Read(ref count);
                Assert.AreEqual(0, curCount);

                timerService.AddEvent(evt);

                Thread.Sleep(500);

                curCount = Interlocked.Read(ref count);

                runtime.Stop();

                Assert.GreaterOrEqual(curCount, 3);
            }
        }

        [Test]
        public void Event_Starts_At_Preset_Time()
        {
            long count = 0;

            TimerRuntimeService timerService = new TimerRuntimeService();
            timerService.AddEvent(new TimerEvent(() => { Interlocked.Increment(ref count); }, TimeSpan.FromMilliseconds(100), DateTime.Now.AddSeconds(5)));

            DeliveryCore deliveryCore = new DirectDeliveryCore();

            using (ServiceBusRuntime runtime = new ServiceBusRuntime(deliveryCore, timerService))
            {                
                runtime.Start();

                Thread.Sleep(3000);

                long curCount = Interlocked.Read(ref count);
                Assert.AreEqual(0, curCount);


                Thread.Sleep(3000);

                curCount = Interlocked.Read(ref count);

                runtime.Stop();

                Assert.GreaterOrEqual(curCount, 0);
            }
        }
        
    }
}
