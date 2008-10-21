using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Timers;

namespace IServiceOriented.ServiceBus.Services
{
    public sealed class TimerRuntimeService : RuntimeService
    {
        protected override void OnStart()
        {
            base.OnStart();

            lock (_timerEvents)
            {
                foreach (TimerEvent evt in _timerEvents)
                {
                    evt.Start();
                }
            }
        }

        protected override void OnStop()
        {
            lock (_timerEvents)
            {
                foreach (TimerEvent evt in _timerEvents)
                {
                    evt.Stop();
                }
            }

            base.OnStop();
        }
        
        public void AddEvent(TimerEvent evt)
        {
            lock (_timerEvents)
            {
                _timerEvents.Add(evt);
                if (Started)
                {
                    evt.Start();
                }
            }
        }

        public void RemoveEvent(TimerEvent evt)
        {
            lock (_timerEvents)
            {
                _timerEvents.Remove(evt);
                evt.Stop();
            }
        }

        List<TimerEvent> _timerEvents = new List<TimerEvent>();

        public IEnumerable<TimerEvent> ListRegisteredEvents()
        {
            lock (_timerEvents)
            {
                return _timerEvents.ToArray();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (TimerEvent t in _timerEvents)
                {
                    t.Dispose(disposing);
                }
                base.Dispose(disposing);
            }
        }
    }

    public sealed class TimerEvent : IDisposable
    {        
        public TimerEvent(Action action, TimeSpan interval) : this(action, interval, null)
        {
            
        }

        public TimerEvent(Action action, TimeSpan interval, object state)
            : this(Guid.NewGuid(), action, interval, DateTime.MinValue, state)
        {
        }

        public TimerEvent(Action action, TimeSpan interval, DateTime startDate) : this(action, interval, startDate, null)
        {
            
        }


        public TimerEvent(Action action, TimeSpan interval, DateTime startDate, object state) : this(Guid.NewGuid(), action, interval, startDate, state)
        {
        }

        public TimerEvent(Guid eventId, Action action, TimeSpan interval, DateTime startDate, object state)
        {
            if (action == null) throw new ArgumentNullException("Action must not be null.");
            if (interval == TimeSpan.Zero)
            {
                throw new ArgumentException("Interval must not be zero");
            }
            EventId = eventId;
            Action = action;
            StartDate = startDate;
            Interval = interval;
            State = state;            
        }

        public Guid EventId
        {
            get;
            private set;
        }

        public DateTime StartDate
        {
            get;
            private set;
        }

        public Action Action
        {
            get;
            private set;
        }

        public TimeSpan Interval
        {
            get;
            private set;
        }

        public object State
        {
            get;
            private set;
        }

        internal void Start()
        {
            if (_disposed) throw new ObjectDisposedException("TimerEvent");

            DateTime now = DateTime.Now;
            if (StartDate > now)
            {
                _timer = new Timer((StartDate - now).TotalMilliseconds);
                _timer.AutoReset = false;
            }
            else
            {
                _timer = new Timer(Interval.TotalMilliseconds);
                _timer.AutoReset = true;
            }
            _timer.Elapsed += new ElapsedEventHandler(onTimerElapsed);
            _timer.Start();
        }

        void onTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_timer.AutoReset)
            {
                _timer.Interval = Interval.TotalMilliseconds;
                _timer.AutoReset = true;
                _timer.Start();
            }
            Action action = Action;
            if (action != null)
            {
                _current = this;
                try
                {
                    action();
                }
                finally
                {
                    _current = null;
                }
            }
        }

        [ThreadStatic]
        static TimerEvent _current;
        public static TimerEvent Current
        {
            get
            {
                return _current;
            }
        }

        internal void Stop()
        {
            if (_disposed) throw new ObjectDisposedException("TimerEvent");

            if (_timer != null)
            {
                _timer.Stop();                
            }
        }

        Timer _timer;
        bool _disposed;

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                if (_timer != null)
                {
                    _timer.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TimerEvent()
        {
            Dispose(false);
        }
    }    

}
