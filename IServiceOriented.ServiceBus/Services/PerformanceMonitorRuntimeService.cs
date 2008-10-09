using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IServiceOriented.ServiceBus.Services
{
    /// <summary>
    /// Provides performance counter support for a service bus instance.
    /// </summary>
    public class PerformanceMonitorRuntimeService : RuntimeService
    {
        public PerformanceMonitorRuntimeService() : this (null, null)
        {
        }
        public PerformanceMonitorRuntimeService(string instanceName, string categoryName)
        {
            if (instanceName == null)
            {
                instanceName = "Default Instance";
            }
            if (categoryName == null)
            {
                categoryName = DEFAULT_CATEGORY_NAME;
            }
            
            _categoryName = categoryName;
            _instanceName = instanceName;
            
        }

        string _categoryName;
        string _instanceName;

        const string DEFAULT_CATEGORY_NAME = "Service Bus";

        const string DELIVERY_COUNTER_NAME = "Message Deliveries";
        const string FAILURE_COUNTER_NAME = "Message Delivery Failures (Retry)";
        const string PERM_FAILURE_COUNTER_NAME = "Message Delivery Failures (Non-retry)";

        const string DELIVERYPS_COUNTER_NAME = "Message Deliveries Per Second";
        const string FAILUREPS_COUNTER_NAME = "Message Delivery Failures Per Second (Retry)";
        const string PERM_FAILUREPS_COUNTER_NAME = "Message Delivery Failures Per Second (Non-retry)";

        protected override void OnStart()
        {
            base.OnStart();

            if (!PerformanceCounterCategory.Exists(_categoryName, System.Environment.MachineName))
            {
                if (AutoCreateCounters)
                {
                    CreateCounters(_categoryName, System.Environment.MachineName);
                }
                else
                {
                    throw new InvalidOperationException("Required performance counters do not exist. Use PerformanceMonitorRuntimeService.Create to create them, or set AutoCreateCounters to true");
                }
            }
            _deliveryCounter = new PerformanceCounter(_categoryName, DELIVERY_COUNTER_NAME , _instanceName, false);
            _failureCounter = new PerformanceCounter(_categoryName, FAILURE_COUNTER_NAME, _instanceName, false);
            _permFailureCounter = new PerformanceCounter(_categoryName, PERM_FAILURE_COUNTER_NAME, _instanceName, false);

            _deliveryPerSecondCounter = new PerformanceCounter(_categoryName, DELIVERYPS_COUNTER_NAME, _instanceName, false);
            _failurePerSecondCounter = new PerformanceCounter(_categoryName, FAILUREPS_COUNTER_NAME, _instanceName, false);
            _permFailurePerSecondCounter = new PerformanceCounter(_categoryName, PERM_FAILUREPS_COUNTER_NAME, _instanceName, false);

        }

        /// <summary>
        /// Create required performance counters
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="machineName"></param>
        public static void CreateCounters(string categoryName, string machineName)
        {
            if (machineName == null)
            {
                machineName = Environment.MachineName;
            }
            if (categoryName == null)
            {
                categoryName = DEFAULT_CATEGORY_NAME;
            }
            CounterCreationDataCollection creationData = new CounterCreationDataCollection();
            creationData.Add(new CounterCreationData(PERM_FAILUREPS_COUNTER_NAME, PERM_FAILUREPS_COUNTER_NAME, PerformanceCounterType.RateOfCountsPerSecond32));
            creationData.Add(new CounterCreationData(FAILUREPS_COUNTER_NAME, FAILUREPS_COUNTER_NAME, PerformanceCounterType.RateOfCountsPerSecond32));
            creationData.Add(new CounterCreationData(DELIVERYPS_COUNTER_NAME, DELIVERYPS_COUNTER_NAME, PerformanceCounterType.RateOfCountsPerSecond32));

            creationData.Add(new CounterCreationData(PERM_FAILURE_COUNTER_NAME, PERM_FAILURE_COUNTER_NAME, PerformanceCounterType.NumberOfItems64));
            creationData.Add(new CounterCreationData(FAILURE_COUNTER_NAME, FAILURE_COUNTER_NAME, PerformanceCounterType.NumberOfItems64));
            creationData.Add(new CounterCreationData(DELIVERY_COUNTER_NAME, DELIVERY_COUNTER_NAME, PerformanceCounterType.NumberOfItems64));

            
            PerformanceCounterCategory.Create(categoryName, machineName, PerformanceCounterCategoryType.MultiInstance, creationData);
        }

        /// <summary>
        /// Specifies whether counters should be automatically created.
        /// </summary>
        /// <remarks>
        /// Peformance counters can only be created with the appropriate permissions. If the bus is running with restricted permissions, counters should be created ahead of time using CreateCounters or the command line tool.
        /// </remarks>
        public bool AutoCreateCounters
        {
            get;
            set;
        }

        protected override void OnStop()
        {
            base.OnStop();

            if(_deliveryCounter != null) _deliveryCounter.Dispose();
            if (_failureCounter != null) _failureCounter.Dispose();
            if (_permFailureCounter != null) _permFailureCounter.Dispose();

            if (_deliveryPerSecondCounter != null) _deliveryPerSecondCounter.Dispose();
            if (_failurePerSecondCounter != null) _failurePerSecondCounter.Dispose();
            if (_permFailurePerSecondCounter != null) _permFailurePerSecondCounter.Dispose();
         
            _permFailureCounter = null;
            _failureCounter = null;
            _deliveryCounter = null;

        }


        PerformanceCounter _deliveryCounter;
        PerformanceCounter _failureCounter;
        PerformanceCounter _permFailureCounter;
        PerformanceCounter _deliveryPerSecondCounter;
        PerformanceCounter _failurePerSecondCounter;
        PerformanceCounter _permFailurePerSecondCounter;

        protected internal override void OnMessageDelivered(MessageDelivery delivery)
        {
            base.OnMessageDelivered(delivery);
            _deliveryCounter.Increment();
            _deliveryPerSecondCounter.Increment();
        }

        protected internal override void OnMessageDeliveryFailed(MessageDelivery delivery, bool permanent)
        {
            base.OnMessageDeliveryFailed(delivery, permanent);
            if (permanent)
            {
                _permFailureCounter.Increment();
                _permFailurePerSecondCounter.Increment();
            }
            else
            {
                _failureCounter.Increment();
                _failurePerSecondCounter.Increment();
            }
        }
    }
}
