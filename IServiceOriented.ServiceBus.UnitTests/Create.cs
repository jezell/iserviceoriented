using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Delivery;
using System.ServiceModel.Channels;
using IServiceOriented.ServiceBus.Delivery.Formatters;

namespace IServiceOriented.ServiceBus.UnitTests
{
    public static class Create
    {
        public static ServiceBusRuntime MemoryQueueRuntime()
        {
            ServiceBusRuntime runtime = new ServiceBusRuntime(new QueuedDeliveryCore(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue()));
            return runtime;
        }
        

        public static ServiceBusRuntime MsmqRuntime<T>()
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


            BinaryMessageEncodingBindingElement element = new BinaryMessageEncodingBindingElement();
            MessageEncoder encoder = element.CreateMessageEncoderFactory().Encoder;

            MessageDeliveryFormatter formatter = new MessageDeliveryFormatter(new ConverterMessageDeliveryReaderFactory<T>(encoder), new ConverterMessageDeliveryWriterFactory<T>(encoder));            
            
            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath, formatter);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath, formatter);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath, formatter);

            return new ServiceBusRuntime(new QueuedDeliveryCore(testQueue, retryQueue, failQueue));
        }

        public static ServiceBusRuntime BinaryMsmqRuntime()
        {
            // Drop test queues if they already exist
            if (MsmqMessageDeliveryQueue.Exists(_testQueuePath))
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

            var binaryFormatter = new System.Messaging.BinaryMessageFormatter();
            MsmqMessageDeliveryQueue testQueue = new MsmqMessageDeliveryQueue(_testQueuePath, binaryFormatter);
            MsmqMessageDeliveryQueue retryQueue = new MsmqMessageDeliveryQueue(_retryQueuePath, binaryFormatter);
            MsmqMessageDeliveryQueue failQueue = new MsmqMessageDeliveryQueue(_failQueuePath, binaryFormatter);

            return new ServiceBusRuntime(new QueuedDeliveryCore(testQueue, retryQueue, failQueue));
        }

        static string _testQueuePath = Config.TestQueuePath;
        static string _retryQueuePath = Config.RetryQueuePath;
        static string _failQueuePath = Config.FailQueuePath;
        
    }
}
