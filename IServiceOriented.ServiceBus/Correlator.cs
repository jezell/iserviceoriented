using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.ObjectModel;

namespace IServiceOriented.ServiceBus
{
    internal class Correlator
    {
        // Todo: replace with something that performs
        IDictionary<string, IAsyncResult> _replies = new Dictionary<string, IAsyncResult>();

        void register(string correlationId, IAsyncResult result)
        {
            lock (_replies)
            {
                _replies.Add(correlationId, result);
            }
        }

        void unregister(string correlationId)
        {
            lock (_replies)
            {
                _replies.Remove(correlationId);
            }
        }


        // Todo: do we need to support wait for multiple deliveries?
        public void Reply(string correlationId, MessageDelivery delivery)
        {
            IAsyncResult result;

            _replies.TryGetValue(correlationId, out result);

            if (result != null)
            {
                CorrelatorAsyncResult car = (CorrelatorAsyncResult)result;
                List<MessageDelivery> deliveries = new List<MessageDelivery>();
                deliveries.Add(delivery);
                car.Complete(new ReadOnlyCollection<MessageDelivery>(deliveries));
            }
        }


        // Todo: need expiration support
        public IAsyncResult BeginWaitForReply(string correlationId, AsyncCallback callback, object o)
        {
            CorrelatorAsyncResult result = new CorrelatorAsyncResult(correlationId, callback, o);
            register(correlationId, result);
            return result;
        }

        public void EndWaitForReply(IAsyncResult result)
        {            
            CorrelatorAsyncResult car = (CorrelatorAsyncResult)result;
            result.AsyncWaitHandle.WaitOne(); // todo: this should have a timeout
            unregister(car.CorrelationId);
        }

    }


    public class CorrelatorAsyncResult : IAsyncResult, IDisposable
    {
        public CorrelatorAsyncResult(string correlationId, AsyncCallback callback, object state)
        {
            Callback = callback;
            CorrelationId = correlationId;
            AsyncState = state;
            AsyncWaitHandle = new ManualResetEvent(false);
        }

        public void Complete(ReadOnlyCollection<MessageDelivery> results)
        {
            Results = results;
            ((ManualResetEvent)AsyncWaitHandle).Set();
            if (Callback != null) Callback(this);
        }

        public ReadOnlyCollection<MessageDelivery> Results
        {
            get;
            private set;
        }

        public string CorrelationId
        {
            get;
            private set;
        }

        public AsyncCallback Callback
        {
            get;
            private set;
        }
        #region IAsyncResult Members

        public object AsyncState
        {
            get;
            private set;
        }

        public WaitHandle AsyncWaitHandle
        {
            get;
            private set;
        }

        public bool CompletedSynchronously
        {
            get;
            private set;
        }

        public bool IsCompleted
        {
            get
            {
                return AsyncWaitHandle.WaitOne(0);
            }
        }

        #endregion

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (AsyncWaitHandle != null)
                {
                    AsyncWaitHandle.Close();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        ~CorrelatorAsyncResult()
        {
            Dispose(false);
        }
    }

}
