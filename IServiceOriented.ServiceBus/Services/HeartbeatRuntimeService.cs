using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Dispatchers;
using System.Threading;

namespace IServiceOriented.ServiceBus.Services
{
    public sealed class HeartbeatRuntimeService : RuntimeService
    {
        protected override void OnValidate()
        {
            getTimerService();   
        }

        protected override void OnStart()
        {
            base.OnStart();            
        }

        private TimerRuntimeService getTimerService()
        {
            try
            {
                TimerRuntimeService timerService = Runtime.ServiceLocator.GetInstance<TimerRuntimeService>();
                return timerService;
            }
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("A TimerRuntimeService instance must be registered with the bus to use the HeartbeatRuntimeService.");
            }    
        }


        protected override void OnStop()
        {
            base.OnStop();
        }

        HashSet<Heartbeat> _heartbeats = new HashSet<Heartbeat>();

        public void RegisterHeartbeat(Heartbeat heartbeat)
        {
            lock (_heartbeats)
            {
                var timerService = getTimerService();                
                _heartbeats.Add(heartbeat);
                TimerEvent evt = new TimerEvent(heartbeatTick, heartbeat.Interval, heartbeat);
                timerService.AddEvent(evt);
                _events.Add(heartbeat, evt);
            }
        }

        void heartbeatTick()
        {
            Heartbeat heartbeat = (Heartbeat)TimerEvent.Current.State;
            heartbeat.Execute(Runtime);
        }

        public void UnregisterHeartbeat(Heartbeat heartbeat)
        {
            lock (_heartbeats)
            {                
                var timerService = getTimerService();
                _heartbeats.Remove(heartbeat);               
                timerService.RemoveEvent(_events[heartbeat]);
                _events.Remove(heartbeat);
            }
        }

        Dictionary<Heartbeat, TimerEvent> _events = new Dictionary<Heartbeat, TimerEvent>();

        public IEnumerable<Heartbeat> ListRegisteredHeartbeats()
        {
            lock (_heartbeats)
            {
                return _heartbeats.ToArray();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                lock (_heartbeats)
                {
                    foreach (Heartbeat h in _heartbeats)
                    {
                        h.Dispose();
                    }
                }
            }
        }
    }

    public sealed class Heartbeat : IDisposable
    {
        private Heartbeat()
        {
            Event = new AutoResetEvent(false);            
        }

        public Heartbeat(Guid heartbeatId, TimeSpan interval, PublishRequest heartbeatRequest, PublishRequest successRequest, PublishRequest failureRequest, MessageFilter responseFilter, TimeSpan timeout) : this()
        {
            if (failureRequest == null)
            {
                throw new ArgumentNullException("failureRequest");
            }

            if (heartbeatRequest == null)
            {
                throw new ArgumentNullException("heartbeatRequest");
            }

            if (responseFilter == null)
            {
                throw new ArgumentNullException("responseFilter");
            }

            if (interval == TimeSpan.Zero)
            {
                throw new ArgumentException("Interval must not be zero.");
            }

            if (timeout == TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout must not be zero.");
            }

            HeartbeatId = heartbeatId;
            Interval = interval;
            HearbeatRequest = heartbeatRequest;
            SuccessRequest = successRequest;
            FailureRequest = failureRequest;
            ResponseFilter = responseFilter;
            Timeout = timeout;
            
        }

        public Guid HeartbeatId
        {
            get;
            private set;
        }

        public TimeSpan Interval
        {
            get;
            private set;
        }

        public PublishRequest HearbeatRequest
        {
            get;
            private set;
        }

        public PublishRequest SuccessRequest
        {
            get;
            private set;
        }

        public PublishRequest FailureRequest
        {
            get;
            private set;
        }

        public MessageFilter ResponseFilter
        {
            get;
            private set;
        }

        public TimeSpan Timeout
        {
            get;
            private set;
        }

        internal AutoResetEvent Event
        {
            get;
            private set;
        }        

        object _executeLock = new object();

        // todo: What if I want to publish a single message, but get multiple responses?
        internal void Execute(ServiceBusRuntime runtime)
        {
            lock (_executeLock)
            {
                Event.Reset();

                SubscriptionEndpoint subscription = new SubscriptionEndpoint(Guid.NewGuid(), "Heartbeat " + HeartbeatId, null, null, HearbeatRequest.ContractType, new HeartbeatReplyDispatcher(this), ResponseFilter, true);
                runtime.Subscribe(subscription);
                try
                {
                    runtime.PublishOneWay(HearbeatRequest);
                    if (Event.WaitOne(Timeout))
                    {
                        // Heartbeat success
                        runtime.PublishOneWay(SuccessRequest);
                    }
                    else
                    {
                        // Hearbeat timeout
                        runtime.PublishOneWay(FailureRequest);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    runtime.Unsubscribe(subscription);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if(disposing)
            {
                if (Event != null)
                {
                    Event.Close();
                }
            }
        }

        ~Heartbeat()
        {
            Dispose(false);
        }
    }

    internal class HeartbeatReplyDispatcher : Dispatcher
    {
        public HeartbeatReplyDispatcher(Heartbeat heartbeat)
        {
            _heartbeat = heartbeat;
        }

        Heartbeat _heartbeat;

        public override void Dispatch(MessageDelivery messageDelivery)
        {
            _heartbeat.Event.Set();
        }
    }
}
