using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Delivery;

namespace IServiceOriented.ServiceBus.UnitTests
{
    public static class Create
    {
        public static ServiceBusRuntime MemoryQueueRuntime()
        {
            ServiceBusRuntime runtime = new ServiceBusRuntime(new QueuedDeliveryCore(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue()));
            return runtime;
        }

        public static ServiceBusRuntime MsmqRuntime()
        {
            // Drop test queues if they already exist
            if(MsmqMessageDeliveryQueue.Exists(_testQueuePath))
            {
                MsmqMessageDeliveryQueue.Delete(_testQueuePath);
            }
            if (MsmqMessageDeliveryQueue.Exists(_retryQueuePath))
            {
                MsmqMessageDeliveryQueue.Delete(_retryQueuePath);
            }
            if (MsmqMessageDeliveryQueue.Exists(_failQueuePath))
            {
                MsmqMessageDeliveryQueue.Delete(_failQueuePath);
            }
            
            // Create test queues
            MsmqMessageDeliveryQueue.Create(_testQueuePath);
            MsmqMessageDeliveryQueue.Create(_retryQueuePath);
            MsmqMessageDeliveryQueue.Create(_failQueuePath);

            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath);

            return new ServiceBusRuntime(new QueuedDeliveryCore(testQueue, retryQueue, failQueue));
        }

        static string _testQueuePath = Config.TestQueuePath;
        static string _retryQueuePath = Config.RetryQueuePath;
        static string _failQueuePath = Config.FailQueuePath;
        
    }
}
